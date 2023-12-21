using System;
using System.Windows.Threading;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// OrbitalSimCmds expposes commands available for the OrbitalSimWindow
    /// </summary>
    public class OrbitalSimCmds
    {

        #region Properties

        readonly System.Windows.Threading.Dispatcher Dispatcher;

        #endregion

        /// <summary>
        /// dispatcher - Dispatch commands to Sim window
        /// </summary>
        /// <param name="dispatcher"></param>
        ///
        public OrbitalSimCmds(System.Windows.Threading.Dispatcher dispatcher)
        {
            Dispatcher = dispatcher;
        }

        #region Axis
        public delegate void AxisDelegate(object[] args);
        private AxisDelegate? _AxisDelegate = null;
        public void AxisRegister(AxisDelegate aDelegate)
        {
            _AxisDelegate = aDelegate;
        }

        public void Axis(bool show)
        {

            if (_AxisDelegate is not null)
            {
                object[] args = { show };
                Dispatcher?.BeginInvoke(DispatcherPriority.Normal, _AxisDelegate, args);
            }
        }
        #endregion

        #region Wireframe
        public delegate void WireframeDelegate(object[] args);
        private WireframeDelegate? _WireframeDelegate = null;
        public void WireframeRegister(WireframeDelegate aDelegate)
        {
            _WireframeDelegate = aDelegate;
        }

        public void Wireframe(bool show)
        {

            if (_WireframeDelegate is not null)
            {
                object[] args = { show };
                Dispatcher?.BeginInvoke(DispatcherPriority.Normal, _WireframeDelegate, args);
            }
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

        #region Camera orbit operations

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
