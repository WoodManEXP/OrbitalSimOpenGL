using OpenTK.Mathematics;
using OpenTK.Wpf;
using System;
using System.Configuration;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using static OrbitalSimOpenGL.SimCamera;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class OrbitalSimWindow : Window
    {
        #region Properties
        public OrbitalSimCmds? OrbitalSimCmds { get; set; }
        private EphemerisBodyList? EphemerisBodyList { get; set; }
        public SimBodyList? SimBodyList { get; set; }
        public SimCamera? SimCamera { get; set; }
        private SimModel SimModel { get; set; }
        private ToolTipHelper ToolTipHelper { get; set; } = new();
        private bool SimRunning { get; set; } = false;
        private bool SimHasBeenStarted { get; set; } = false;
        private String AppDataFolder { get; set; }

        private bool OnLoadedYet = false;

        /// <summary>
        /// Static reference to the one OrbitalSimWindow in the app.
        /// </summary>
        private static OrbitalSimWindow? ThisOrbitalSimWindow { get; set; }

        // Commands
        public enum GenericCommands
        {
            Axis
          , Wireframe
          , Reticle
          , Keep
        };

        private Stats Stats { get; set; }
        #endregion

        public OrbitalSimWindow()
        {
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
            var appName = assembly.GetName().Name;

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AppDataFolder = Path.Combine(localAppData, appName); // appDataFolder = "C:\\Users\\Robert\\AppData\\Local\\OrbitalSimOpenGL"

            InitializeComponent();

            OrbitalSimCmds = new OrbitalSimCmds(Dispatcher);

            // Register command delegates
            OrbitalSimCmds.ScaleCameraRegister(ScaleCamera);
            OrbitalSimCmds.MoveCameraRegister(MoveCamera);
            OrbitalSimCmds.GoNearRegister(GoNear);
            OrbitalSimCmds.LookCameraRegister(LookCamera);
            OrbitalSimCmds.LookAtCameraRegister(LookAtCamera);
            OrbitalSimCmds.TiltCameraRegister(TiltCamera);
            OrbitalSimCmds.OrbitCameraRegister(OrbitCamera);
            OrbitalSimCmds.OrbitAboutRegister(OrbitAbout);

            OrbitalSimCmds.StartSimRegister(StartSim);
            OrbitalSimCmds.PauseSimRegister(PauseSim);

            OrbitalSimCmds.SimIterationRateRegister(IterationRate);
            OrbitalSimCmds.SimTimeCompressionRegister(TimeCompression);

            OrbitalSimCmds.GenericRegister(GenericCommand);

            SimModel = new();

            Stats = new(this, SimModel);
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
        }

        #region Frame rendering
        readonly IntMovingAverage FrameRateMovingAverage = new(50); // Moving average from last N calls
        int UpdateFrameLastMS = 0;

        /// <summary>
        /// 
        /// </summary>
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
            SimCamera?.AnimateCamera(ms, frameRateMS);

            SimCamera?.Render(); // In case camera needs to render (e.g. recticle)

            Stats.Render(timeSpan, frameRateMS);
        }
        #endregion

        #region Commands
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
            CameraOrbitDirections orbitDirection = (CameraOrbitDirections)args[0];
            Single degrees = (Single)args[1];
            //System.Diagnostics.Debug.WriteLine("TiltCamera " + tiltDirection.ToString());
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
        // Window loaded
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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

            // Make Keep settings

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

                // Instantiate model elements
                SimModel.InitScene(EphemerisBodyList);

                // Camera
                Double cX = -1 * 6.0E06D, cY = 3 * 6.0E06D, cZ = 3 * 6.0E06D;
                SimCamera = SimModel.SimCamera = Stats.SimCamera =
                        new(SimModel,
                            new Vector3d(cX, cY, cZ), new Vector3d(0d, 0d, 0d),
                            OpenTkControl.ActualWidth, OpenTkControl.ActualHeight);

                SimCamera.OrbitAbout(-1); // Initially orbit about system's origin
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

        private void GenericCommand(object[] args)
        {
            GenericCommands cmd;

            if (SimModel is not null)
                switch (cmd = (GenericCommands)args[0])
                {
                    case GenericCommands.Axis:
                        SimModel.IncludeAxis = (bool)args[1];
                        break;

                    case GenericCommands.Wireframe:
                        SimModel.Wireframe = (bool)args[1];
                        break;

                    case GenericCommands.Keep:
                        SimModel.SimCamera.KeepKind = (SimCamera.KindOfKeep)args[1];
                        break;

                    case GenericCommands.Reticle:
                        SimModel.SimCamera.ShowReticle = (bool)args[1];
                        break;

                    default:
                        break;
                }
        }

        private void UseWireframe(object[] args)
        {
            bool b = (bool)args[0];
            if (SimModel is not null)
                SimModel.Wireframe = b;
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

        private static bool WindowWasResized = false;

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
            MousePosition.X =  MousePosition.Y = -1;
        }
    }
}
