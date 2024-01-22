using System;
using System.Windows.Threading;
using static OrbitalSimOpenGL.OrbitalSimWindow;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Exposes commands available for the OrbitalSimWindow
    /// </summary>
    public class CommandSimWindow : CommandDelegate
    {

        #region Properties

        // Commands
        public enum GenericCommands
        {
            Axis
          , Wireframe
          , Reticle
          , Keep
          , GravConstant
          , ExcludeBody
          , TracePath
          , MassMultiplier
          , VelocityMultiplier
          , ResetSim
        };
        #endregion

        /// <summary>
        /// dispatcher - Dispatch commands to Sim window
        /// </summary>
        /// <param name="dispatcher"></param>
        ///
        public CommandSimWindow(System.Windows.Threading.Dispatcher dispatcher)
            : base(dispatcher)
        {
        }

        #region TracePath
        /// <summary>
        /// Have a body turn on/off retain and trace its travel path
        /// </summary>
        /// <param name="show"></param>
        /// <param name="bodyName"></param>
        public void TracePath(String bodyName, bool show)
        {
            object[] args = { CommandSimWindow.GenericCommands.TracePath, show, bodyName };
            GenericCommand(args);
        }
        #endregion

        #region Reset
        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        public void ResetSim()
        {
            object[] args = { CommandSimWindow.GenericCommands.ResetSim };
            GenericCommand(args);
        }
        #endregion

        #region Exclude Body
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bodyName"></param>
        public void ExcludeBody(String bodyName)
        {
            object[] args = { CommandSimWindow.GenericCommands.ExcludeBody, bodyName };
            GenericCommand(args);
        }
        #endregion

        #region MassMultiplier
        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        public void MassMultiplier(String body, int massMultiplier)
        {
            object[] args = { CommandSimWindow.GenericCommands.MassMultiplier, body, massMultiplier };
            GenericCommand(args);
        }
        #endregion

        #region VelocityMultiplier
        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        public void VelocityMultiplier(String body, int velocityMultiplier)
        {
            object[] args = { CommandSimWindow.GenericCommands.VelocityMultiplier, body, velocityMultiplier };
            GenericCommand(args);
        }
        #endregion


        #region Grav Constant
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"><0 divides GC by that value, >0 multiplies GC by that value, 0 sets to std value</param>
        public void GravConstant(int value)
        {
            object[] args = { CommandSimWindow.GenericCommands.GravConstant, value };
            GenericCommand(args);
        }
        #endregion

        #region Axis
        public void Axis(bool show)
        {
            object[] args = { CommandSimWindow.GenericCommands.Axis, show };
            GenericCommand(args);
        }
        #endregion

        #region Wireframe
        public void Wireframe(bool show)
        {
            object[] args = { CommandSimWindow.GenericCommands.Wireframe, show };
            GenericCommand(args);
        }
        #endregion

        #region Keep
        public void Keep(SimCamera.KindOfKeep kindOfKeep)
        {
            object[] args = { CommandSimWindow.GenericCommands.Keep, kindOfKeep };
            GenericCommand(args);
        }
        #endregion

        #region Reticle
        public void Reticle(bool show)
        {
            object[] args = { CommandSimWindow.GenericCommands.Reticle, show };
            GenericCommand(args);
        }
        #endregion

        #region Camera Scale 

        public delegate void ScaleCameraDelegate(object[] args);
        private ScaleCameraDelegate? _ScaleCameraDelegate = null;
        public void ScaleCameraRegister(ScaleCameraDelegate aDelegate)
        {
            _ScaleCameraDelegate = aDelegate;
        }

        public void ScaleCamera(Double scale)
        {

            if (_ScaleCameraDelegate is not null)
            {
                object[] args = { scale };
                Dispatcher?.BeginInvoke(DispatcherPriority.Normal, _ScaleCameraDelegate, args);
            }
        }
        #endregion

        #region Camera Move operations

        public delegate void MoveCameraDelegate(object[] args);
        private MoveCameraDelegate? _MoveCameraDelegate = null;
        public delegate void GoNearDelegate(object[] args);
        private GoNearDelegate? _GoNearDelegate = null;

        public void MoveCameraRegister(MoveCameraDelegate aDelegate)
        {
            _MoveCameraDelegate = aDelegate;
        }

        public void MoveCamera(SimCamera.CameraMoveDirections moveDirection)
        {

            if (null != _MoveCameraDelegate)
            {
                object[] args = { moveDirection };
                Dispatcher?.BeginInvoke(DispatcherPriority.Normal, _MoveCameraDelegate, args);
            }
        }
        public void GoNearRegister(GoNearDelegate aDelegate)
        {
            _GoNearDelegate = aDelegate;
        }
        public void GoNear(string bodyName)
        {
            if (null != _GoNearDelegate)
            {
                object[] args = { bodyName };
                Dispatcher?.BeginInvoke(DispatcherPriority.Normal, _GoNearDelegate, args);
            }
        }
        #endregion

        #region Camera Look operations

        public delegate void LookCameraDelegate(object[] args);
        public delegate void LookAtCameraDelegate(object[] args);
        private LookCameraDelegate? _LookCameraDelegate = null;
        private LookAtCameraDelegate? _LookAtCameraDelegate = null;

        public void LookCameraRegister(LookCameraDelegate aDelegate)
        {
            _LookCameraDelegate = aDelegate;
        }

        public void LookAtCameraRegister(LookAtCameraDelegate aDelegate)
        {
            _LookAtCameraDelegate = aDelegate;
        }
        public void LookCamera(SimCamera.CameraLookDirections lookDirection, Single degrees)
        {

            if (null != _LookCameraDelegate)
            {
                object[] args = { lookDirection, degrees };
                Dispatcher?.BeginInvoke(DispatcherPriority.Normal, _LookCameraDelegate, args);
            }
        }
        public void LookAtCamera(string lookAt)
        {
            if (null != _LookAtCameraDelegate)
            {
                object[] args = { lookAt };
                Dispatcher?.BeginInvoke(DispatcherPriority.Normal, _LookAtCameraDelegate, args);
            }
        }
        #endregion

        #region Camera Tilt operations

        public delegate void TiltCameraDelegate(object[] args);
        private TiltCameraDelegate? _TiltCameraDelegate = null;

        public void TiltCameraRegister(TiltCameraDelegate aDelegate)
        {
            _TiltCameraDelegate = aDelegate;
        }

        public void TiltCamera(SimCamera.CameraTiltDirections tiltDirection, Single degrees)
        {

            if (null != _TiltCameraDelegate)
            {
                object[] args = { tiltDirection, degrees };
                Dispatcher?.BeginInvoke(DispatcherPriority.Normal, _TiltCameraDelegate, args);
            }
        }
        #endregion

        #region Camera Orbit operations

        public delegate void OrbitCameraDelegate(object[] args);
        private OrbitCameraDelegate? _OrbitCameraDelegate = null;
        public delegate void OrbitAboutDelegate(object[] args);
        private OrbitAboutDelegate? _OrbitAboutDelegate = null;
        public void OrbitCameraRegister(OrbitCameraDelegate aDelegate)
        {
            _OrbitCameraDelegate = aDelegate;
        }

        public void OrbitCamera(SimCamera.CameraOrbitDirections orbitDirection, Single orbitDegrees)
        {

            if (_OrbitCameraDelegate is not null)
            {
                object[] args = { orbitDirection, orbitDegrees };
                Dispatcher?.BeginInvoke(DispatcherPriority.Normal, _OrbitCameraDelegate, args);
            }
        }
        public void OrbitAboutRegister(OrbitAboutDelegate aDelegate)
        {
            _OrbitAboutDelegate = aDelegate;
        }
        public void OrbitAbout(string bodyName)
        {
            if (null != _OrbitAboutDelegate)
            {
                object[] args = { bodyName };
                Dispatcher?.BeginInvoke(DispatcherPriority.Normal, _OrbitAboutDelegate, args);
            }
        }
        #endregion

        #region Start, Pause operations

        public delegate void StartSimDelegate(object[] args);
        private StartSimDelegate? _StartSimDelegate = null;
        public void StartSimRegister(StartSimDelegate aDelegate)
        {
            _StartSimDelegate = aDelegate;
        }

        public void StartSim(EphemerisBodyList simBodyList)
        {

            if (null != _StartSimDelegate)
            {
                object[] args = { simBodyList };
                Dispatcher?.BeginInvoke(DispatcherPriority.Normal, _StartSimDelegate, args);
            }
        }
        public delegate void PauseSimDelegate(object[] args);
        private PauseSimDelegate? _PauseSimDelegate = null;
        public void PauseSimRegister(PauseSimDelegate aDelegate)
        {
            _PauseSimDelegate = aDelegate;
        }

        public void PauseSim()
        {

            if (null != _PauseSimDelegate)
            {
                object[] args = { };
                Dispatcher?.BeginInvoke(DispatcherPriority.Normal, _PauseSimDelegate, args);
            }
        }
        #endregion

        #region Sim iteration rate, time compression

        public delegate void SimIterationRateDelegate(object[] args);
        private SimIterationRateDelegate? _SimIterationRateDelegate = null;
        public delegate void SimTimeCompressionDelegate(object[] args);
        private SimTimeCompressionDelegate? _SimTimeCompressionDelegate = null;
        public void SimIterationRateRegister(SimIterationRateDelegate aDelegate)
        {
            _SimIterationRateDelegate = aDelegate;
        }
        public void SimIterationRate(int seconds)
        {
            if (null != _SimIterationRateDelegate)
            {
                object[] args = { seconds };
                Dispatcher?.BeginInvoke(DispatcherPriority.Normal, _SimIterationRateDelegate, args);
            }
        }
        public void SimTimeCompressionRegister(SimTimeCompressionDelegate aDelegate)
        {
            _SimTimeCompressionDelegate = aDelegate;
        }
        public void SimTimeCompression(int compressionRate)
        {
            if (null != _SimTimeCompressionDelegate)
            {
                object[] args = { compressionRate };
                Dispatcher?.BeginInvoke(DispatcherPriority.Normal, _SimTimeCompressionDelegate, args);
            }
        }
        #endregion    
    }
}
