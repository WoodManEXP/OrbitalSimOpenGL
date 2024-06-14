using OpenTK.Mathematics;
using OpenTK.Wpf;
using System;
using System.Configuration;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using static OrbitalSimOpenGL.CommandStatusWindow;
using static OrbitalSimOpenGL.SimCamera;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class OrbitalSimWindow : Window
    {
        #region Properties
        public CommandSimWindow? CommandSimWindow { get; set; }
        private EphemerisBodyList? EphemerisBodyList { get; set; }
        public SimCamera? SimCamera { get; set; }
        private SimModel? SimModel { get; set; }
        public Scale Scale { get; set; } = new(); // Scales U coords to OpenGL coords
        private ToolTipHelper ToolTipHelper { get; set; } = new();
        private bool SimHasBeenStarted { get; set; } = false;
        private String AppDataFolder { get; set; }

        private bool OnLoadedYet = false;

        /// <summary>
        /// Static reference to the one OrbitalSimWindow in the app.
        /// </summary>
        private static OrbitalSimWindow? ThisOrbitalSimWindow { get; set; }

        private StatsArea? StatsArea { get; set; }
        public CommandControlWindow CommandControlWindow { get; set; }
        public CommandStatusWindow CommandStatusWindow { get; set; }
        #endregion

        public OrbitalSimWindow(CommandControlWindow commandControlWindow, CommandStatusWindow commandStatusWindow)
        {
            // For sending commands/info to the controller and status windows
            CommandControlWindow = commandControlWindow;
            CommandStatusWindow = commandStatusWindow;

            ThisOrbitalSimWindow = this; // In order to find this in the static method(s)

            InitializeComponent();

            var settings = new GLWpfControlSettings
            {
                MajorVersion = 4,
                MinorVersion = 2,
                RenderContinuously = true
            };
            OpenTkControl.Start(settings);

            var assembly = System.Reflection.Assembly.GetAssembly(this.GetType());//Get the assembly object
            var appName = assembly?.GetName().Name;

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AppDataFolder = Path.Combine(localAppData, appName); // appDataFolder = "C:\\Users\\Robert\\AppData\\Local\\OrbitalSimOpenGL"

            CommandSimWindow = new(Dispatcher);

            // Register command delegates
            CommandSimWindow.ScaleCameraRegister(ScaleCamera);
            CommandSimWindow.MoveCameraRegister(MoveCamera);
            CommandSimWindow.GoNearRegister(GoNear);
            CommandSimWindow.LookCameraRegister(LookCamera);
            CommandSimWindow.LookAtCameraRegister(LookAtCamera);
            CommandSimWindow.TiltCameraRegister(TiltCamera);
            CommandSimWindow.OrbitCameraRegister(OrbitCamera);
            CommandSimWindow.OrbitAboutRegister(OrbitAbout);

            CommandSimWindow.StartSimRegister(StartSim);
            CommandSimWindow.PauseSimRegister(PauseSim);

            CommandSimWindow.SimIterationRateRegister(IterationRate);
            CommandSimWindow.SimTimeCompressionRegister(TimeCompression);

            CommandSimWindow.GenericRegister(GenericCommand);
        }

        // Window loaded
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            OnLoadedYet = true;

            // Hook in. to detect conditions for synthesis of an after resize event
            // https://stackoverflow.com/questions/4474670/how-to-catch-the-ending-resize-window
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(new HwndSourceHook(WndProc));

            // Camera
            Double cX = -1 * 6.0E06D, cY = 3 * 6.0E06D, cZ = 3 * 6.0E06D;
            SimCamera = new(Scale, new Vector3d(cX, cY, cZ), new Vector3d(0d, 0d, 0d),
                        OpenTkControl.ActualWidth, OpenTkControl.ActualHeight);

            SimModel = SimCamera.SimModel = new(this);
            StatsArea = new(this, SimModel, SimCamera);
        }

        #region Frame rendering
        readonly IntMovingAverage FrameRateMovingAverage = new(50); // Moving average from last N calls
        int UpdateFrameLastMS = 0;
        int EllapsedMS_ApproachDisatances = 0;

        /// <summary>
        /// Rendering and hit-testing
        /// </summary>
        /// <remarks>
        /// Rendering is necessary if
        /// 1. Sim is running, or
        /// 2. Camera is animating, or
        /// 3. Cursor is moving over the graphics area (hit-testing), or
        /// 4. Change has been sent for the display (axis, reticle, wireframe, ...)
        /// Otherwise thus can return immediately.
        /// Whenever it determined rendering is unnecessary FPS indicator, in stats area, should be set to 0.
        /// </remarks>
        /// <param name="timeSpan">Elapsed tine since last render call</param>
        private void OnRender(TimeSpan timeSpan)
        {
            // Let OnLoaded get called before beginning rendering
            if (!OnLoadedYet)
                return;

            if (!SimModel.SceneReady)
                return;

            //int minutes = timeSpan.Minutes;
            //int seconds = timeSpan.Seconds;
            //int milliseconds = timeSpan.Milliseconds;

            int ms = timeSpan.Milliseconds;

            if (0 == UpdateFrameLastMS)
            {
                UpdateFrameLastMS = ms;
                return; // No frame rate yet --> skip the first frame
            }
            else
                UpdateFrameLastMS = ms;

            // FrameRate, a frame is happening on average every framerateMS
            int frameRateMS = FrameRateMovingAverage.AnotherValue(ms);

            SimModel.Render(ms, frameRateMS, MousePosition);

            // Perform any active camera animations.
            // Camera animation occurs after SimModel.Render as it moves things in the model space
            if (SimCamera is not null)
            {
                SimCamera.AnimateCamera(ms, frameRateMS);
                SimCamera.Render(); // In case camera needs to render (e.g. recticle)
            }

            StatsArea?.Render(timeSpan, frameRateMS);

            // *** Send closest approach stats to Stats Windows ***
            // If at least one body is displaying approach distances then once every n, real-time seconds
            EllapsedMS_ApproachDisatances += ms;
            if (SimModel.NumApproachDistancesSet > 0 && EllapsedMS_ApproachDisatances >= 2000)
            {
                EllapsedMS_ApproachDisatances = 0; // Reset
                ClosestApproachStats();
            }
        }
        #endregion

        #region ClosestApproach stats

        /// <summary>
        /// Prepare and send approach stats to OrbitalSimStatusWindow
        /// </summary>
        private void ClosestApproachStats()
        {

            // Build up the approach information bloc to send to status window
            int numBodies = SimModel.ApproachInfo.NumBodies;
            int numApproachDistancesSet = SimModel.NumApproachDistancesSet;

            ApproachStatus approachStatus = new(EphemerisBodyList.EphemerisDateTime, numApproachDistancesSet, numBodies);
            ApproachElements approachElements = SimModel.ApproachInfo.ApproachElements;

            // Copy info from ApproachDistances to approachStatus.
            // ApproachDistances maintained in this thread.
            // ApproachStatus will be seralized and sent to another thread.
            int slot = -1;
            int lBody = -1;
            SparseArray sparseArray = SimModel.SparseArray;
            foreach (SimBody sB in SimModel.SimBodyList.BodyList)
            {
                lBody++;

                if (sB.ExcludeFromSim)
                    continue;

                if (!sB.DisplayApproaches)
                    continue;

                if (++slot >= numApproachDistancesSet)
                    continue; // JIC

                ref ApproachStatusBody approachStatusBody = ref approachStatus.ApproachStatusInfo.ApproachStatusBody[slot];

                approachStatusBody.Name = new(sB.Name);

                int j = -1; // There are numBodies-1 elements in the approachStatusBody.ApproachElements array
                for (int hBody = 0;hBody < numBodies;hBody++)
                {
                    if (lBody == hBody)
                        continue;

                    j++;

                    int i = sparseArray.ValuesIndex(lBody, hBody);

                    ref ApproachElement approachElement = ref approachStatusBody.ApproachElements[j];

                    SimBody hSB = SimModel.SimBodyList.BodyList[hBody];
                    approachElement = approachElements.Elements[i];
                    approachElement.Name = new(hSB.Name);
                }
            }

            // Serialize and send info across thread boundary to Status Window (via event queue)
            String approachStatusStr = approachStatus.Serialize();
            CommandStatusWindow.ApproachDist(approachStatusStr);
        }
        #endregion

        #region Commands

        /// <summary>
        /// Command coming in from somewhere on the message queue
        /// </summary>
        /// <param name="args"></param>
        private void GenericCommand(object[] args)
        {
            if (SimModel is not null)
                switch ((CommandSimWindow.GenericCommands)args[0])
                {
                    case CommandSimWindow.GenericCommands.Axis:
                        SimModel.ShowAxis = (bool)args[1];
                        break;

                    case CommandSimWindow.GenericCommands.Wireframe:
                        SimModel.Wireframe = (bool)args[1];
                        break;

                    case CommandSimWindow.GenericCommands.Barycenter:
                        SimModel.ShowBarycenter = (bool)args[1];
                        break;

                    case CommandSimWindow.GenericCommands.Keep:
                        if (SimModel.SimCamera is not null)
                            SimModel.SimCamera.KeepKind = (SimCamera.KindOfKeep)args[1];
                        break;

                    case CommandSimWindow.GenericCommands.Reticle:
                        if (SimModel.SimCamera is not null)
                            SimModel.SimCamera.ShowReticle = (bool)args[1];
                        break;

                    case CommandSimWindow.GenericCommands.GravConstant:
                        SimModel.GravConstant((int)args[1]);
                        break;

                    case CommandSimWindow.GenericCommands.ExcludeBody:
                        SimModel.ExcludeBody((String)args[1]);              // Tell the model (sometimes a NOp)
                                                                            //                        if (SimModel.SimBodyList is not null)
                                                                            //                            CommandControlWindow.ExcludeBody(SimModel.SimBodyList.GetIndex((String)args[1]));  // Tell CommandControlWindow
                        break;

                    case CommandSimWindow.GenericCommands.MassMultiplier:
                        SimModel.SetMassMultiplier((String)args[1], (int)args[2]);
                        break;

                    case CommandSimWindow.GenericCommands.VelocityMultiplier:
                        SimModel.SetVelocityMultiplier((String)args[1], (int)args[2]);
                        break;

                    case CommandSimWindow.GenericCommands.ResetSim:
                        ResetSim((bool)args[1]);
                        break;

                    case CommandSimWindow.GenericCommands.TracePath:
                        SimModel.TracePath((bool)args[1], (String)args[2]);
                        break;

                    case CommandSimWindow.GenericCommands.DetectCollisions:
                        SimModel.DetectCollisions((bool)args[1]);
                        break;

                    case CommandSimWindow.GenericCommands.ApproachDistance:
                        SimModel.DisplayApproachDistance((String)args[1], (bool)args[2]);
                        break;

                    default:
                        break;
                }
        }

        private void TimeCompression(object[] args)
        {
            int compressionRate = (int)args[0];
            SimModel.TimeCompression = compressionRate;
        }

        private void IterationRate(object[] args)
        {
            int seconds = (int)args[0];
            SimModel.IterationSeconds = seconds; // Set the property in the model
        }

        /// <summary>
        /// Initiate camera orbit around a system body, or system origin
        /// </summary>
        /// <param name="args"></param>
        private void OrbitCamera(object[] args)
        {
            // If the body to be orbited, SimCamera.OrbitBodyIndex, is not the same as the current 
            // Keep body, SimCamera.KeepBody, disable Keep.
            if (SimCamera.OrbitTurnOffKeep())
                CommandControlWindow.KeepOff(); // Inform controller of Keep disable

            CameraOrbitDirections orbitDirection = (CameraOrbitDirections)args[0];
            Single degrees = (Single)args[1];
            SimCamera?.OrbitCamera(orbitDirection, degrees);
        }

        /// <summary>
        /// Orbit About
        /// Set which system body,or system origin, about which to orbit camera
        /// </summary>
        /// <param name="args"></param>
        private void OrbitAbout(object[] args)
        {
            String bodyName = (String)args[0];
            int index;

            if (bodyName.Equals(Properties.Settings.Default.Origin))
                index = -1;
            else
                index = SimModel.SimBodyList.GetIndex(bodyName);

            SimCamera?.OrbitAbout(index);
        }

        /// <summary>
        /// Use between 0 and 20
        /// Scaled movement amounts for U, D, L, R, F, B
        /// </summary>
        /// <param name="args"></param>
        private void ScaleCamera(object[] args)
        {
            Double scale = (Double)args[0];
            if (SimModel is not null)
                SimModel.Scale.CamMoveAmt = (int)scale; // 0 .. 20
        }

        // Move camera
        private void MoveCamera(object[] args)
        {
            CameraMoveDirections moveDirection = (CameraMoveDirections)args[0];
            //System.Diagnostics.Debug.WriteLine("MoveCamera " + moveDirection.ToString());
            SimCamera?.Move(moveDirection);
        }

        // Go Near
        private void GoNear(object[] args)
        {
            String bodyName = (String)args[0];
            int index;

            if (bodyName.Equals(Properties.Settings.Default.Origin))
                index = -1;
            else
                index = SimModel.SimBodyList.GetIndex(bodyName);

            SimCamera?.GoNear(index);

            // Make Keep settings

        }

        // Look camera
        private void LookCamera(object[] args)
        {
            CameraLookDirections lookDirection = (CameraLookDirections)args[0];
            Single degrees = (Single)args[1];

            switch (lookDirection)
            {
                case CameraLookDirections.LookLeft:
                    SimCamera?.Look(-degrees, 0f);
                    break;
                case CameraLookDirections.LookRight:
                    SimCamera?.Look(degrees, 0f);
                    break;
                case CameraLookDirections.LookUp:
                    SimCamera?.Look(0f, -degrees);
                    break;
                case CameraLookDirections.LookDown:
                default:
                    SimCamera?.Look(0f, degrees);
                    break;
            }
        }

        private void TiltCamera(object[] args)
        {
            CameraTiltDirections tiltDirection = (CameraTiltDirections)args[0];
            Single degrees = (Single)args[1];

            //System.Diagnostics.Debug.WriteLine("TiltCamera " + tiltDirection.ToString());
            SimCamera?.Tilt(tiltDirection, degrees);
        }

        // LookAt camera
        private void LookAtCamera(object[] args)
        {
            String lookAtStr = (String)args[0];

            if (lookAtStr.Equals(Properties.Settings.Default.Origin))
                SimCamera?.LookAt(-1);
            else if (lookAtStr.Equals(Properties.Settings.Default.SystemBarycenter))
                // Barycenter, from JPL, not being captured yet, use 0,0,0 for now
                SimCamera?.LookAt(-1);
            else
                SimCamera?.LookAt(SimModel.SimBodyList.GetIndex(lookAtStr));

            // Set Stats to show stats for the LookAt body
            if (SimHasBeenStarted)
            {
                SimBody sB = SimModel.SimBodyList.GetSB(lookAtStr);
                if (sB is not null)
                    SimModel.ShowStatsForSB = sB;
            }
        }

        // Start sim (and continue paused sim)

        private void StartSim(object[] args)
        {
            if (!SimHasBeenStarted)
            {
                SimHasBeenStarted = true;

                // Bodies to be included in sim
                EphemerisBodyList = (EphemerisBodyList)args[0];

                // Let model know about AppDataFolder
                SimModel.AppDataFolder = AppDataFolder;

                // Prep scene
                SimModel.ResetScene(EphemerisBodyList);
            }

            SimModel.SimRunning = true;
            UpdateFrameLastMS = 0; // Reset on each start
            FrameRateMovingAverage.Reset();
        }

        // Pause sim
        private void PauseSim(object[] args)
        {
            SimModel.SimRunning = false;
        }

        /// <summary>
        /// Perform reset of Sim to initial state
        /// </summary>
        private void ResetSim(bool resetCamera)
        {
            if (EphemerisBodyList is null)
                return;

            // Camera to starting position and starting lookVector
            if (resetCamera)
            {
                Double cX = -1 * 6.0E06D, cY = 3 * 6.0E06D, cZ = 3 * 6.0E06D;
                SimCamera?.Reset(new Vector3d(cX, cY, cZ), new Vector3d(0d, 0d, 0d));
            }

            SimModel.ResetScene(EphemerisBodyList);

            StatsArea.Reset();
        }
        #endregion

        #region Hit testing
        public System.Windows.Point MousePosition; // Integer coords

        /// <summary>
        /// Handle MouseMoves in the OpenTK control in SimWindow
        /// See https://stackoverflow.com/questions/21750692/viewport3d-mouse-event-doesnt-fire-when-hitting-background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Retrieve the coordinate of the mouse position.
            MousePosition = e.GetPosition((UIElement)sender);

            //System.Diagnostics.Debug.WriteLine("MouseMove " + MousePosition.ToString());
        }
        #endregion

        #region Window resize
        private void AspectRatoChanged()
        {
            if (SimHasBeenStarted)
                SimCamera.SetAspectRatio(OpenTkControl.ActualWidth, OpenTkControl.ActualHeight);
        }

        //const int WM_SIZING = 0x214;
        const int WM_EXITSIZEMOVE = 0x232;
        const int WM_ENTERSIZEMOVE = 0x0231;

        private static bool WindowWasResized = false; // static ?

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (WM_ENTERSIZEMOVE == msg)
                if (!WindowWasResized)
                    WindowWasResized = true;
                else;
            else
            if (WM_EXITSIZEMOVE == msg)
                // Had WM_ENTERSIZEMOVE been encountered          
                if (WindowWasResized)
                {
                    // Let OrbitalSimWindow have a go at it
                    OrbitalSimWindow.ThisOrbitalSimWindow.AspectRatoChanged();

                    // Ready for the next resize/move
                    WindowWasResized = false;
                }

            return IntPtr.Zero;
        }
        #endregion

        private void MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MousePosition.X = MousePosition.Y = -1;
        }
    }
}
