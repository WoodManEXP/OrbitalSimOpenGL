using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    //https://opentk.net/learn/chapter1/9-camera.html?tabs=input-opentk4%2Cdelta-time-input-opentk4%2Ccursor-mode-opentk4%2Cmouse-move-opentk4%2Cscroll-opentk4
    public class SimCamera
    {
        public enum CameraMoveDirections
        {
            MoveForward, MoveBackward, MoveUp, MoveDown, MoveLeft, MoveRight
        };
        public enum CameraLookDirections
        {
            LookLeft, LookRight, LookUp, LookDown
        };
        public enum CameraTiltDirections
        {
            TiltClockwise, TileCounterClockwise
        };
        public enum CameraOrbitDirections
        {
            OrbitUp, OrbitDown, OrbitLeft, OrbitRight
        };

        #region Properties
        //readonly static float ToRadians = Math.PI / 180D;
        //readonly static Double ToDegrees = 180D / Math.PI;
        static Double OneAU { get; } = 1.49668992E8D; // KM (93M miles);

        // Amount camera moves increses exponentially as the scale increases (0 .. N)
        public Double[] CamMoveKMs { get; }
        public Double DepthNear { get; set; } = 1E3D; // U coords
        public Double DepthFar { get; set; } = 12E9D; // U coords
        public float FieldOfView { get; set; } = 60.0f * (MathHelper.Pi / 180f); // Radians

        private float _aspectRatio = 1.0f;
        public float AspectRatio
        {
            get { return _aspectRatio; }
            set
            {
                _aspectRatio = value;
                // Projection matrix changes when aspect ratio changes
                // On average, Pluto is 3.7 billion miles (5.9 billion kilometers) away from the Sun
                // (hence depth of field of view)
                ProjectionMatrixD = Matrix4d.CreatePerspectiveFieldOfView(FieldOfView, _aspectRatio,
                                           Scale.ScaleU_ToW(DepthNear), Scale.ScaleU_ToW(DepthFar));
                Matrix4D_toS(ref _ProjectionMatrixD, ref _ProjectionMatrix);
                Reticle.AspectRatio = _aspectRatio; // Tell the Reticle
            }
        }

        // Keep properties
        public enum KindOfKeep
        {
            None
          , LookAt              // Camera keeps looking at last LookAt or GoNear choice
          , LookAtAndDistance   // LookAt + camera maintains current distance
        };
        private int KeepBody { get; set; } = -2; // -2 is nothing set

        private bool _KeepIsOnStation = false;
        private bool KeepIsOnStation // Camera currently being kept On Station
        {
            get => _KeepIsOnStation;
            set { _KeepIsOnStation = value; }
        }
        private KindOfKeep _KeepKind = KindOfKeep.None;
        private Vector3d KeepPosition;
        public KindOfKeep KeepKind
        {
            get { return _KeepKind; }
            set
            {
                _KeepKind = value;
                if (KindOfKeep.None == _KeepKind)
                {
                    KeepIsOnStation = false;
                }
            }
        }
        public Single ViewWidth { get; set; }
        public Single ViewHeight { get; set; }

        // Single precision versions of ViewMatrix, ProjectionMatrix, and VP_Matrix
        public Matrix4 _ProjectionMatrix = Matrix4.Identity, _ViewMatrix = Matrix4.Identity, _VP_Matrix = Matrix4.Identity;
        public Matrix4 ProjectionMatrix { get { return _ProjectionMatrix; } private set { _ProjectionMatrix = value; } }
        public Matrix4 ViewMatrix { get { return _ViewMatrix; } private set { _ViewMatrix = value; } }
        public Matrix4 VP_Matrix { get { return _VP_Matrix; } private set { _VP_Matrix = value; } } // View * Projection

        // Double precision version of ViewMatrix, ProjectionMatrix, and VP_Matrix
        private Matrix4d _ProjectionMatrixD = Matrix4d.Identity, _ViewMatrixD = Matrix4d.Identity, _VP_MatrixD = Matrix4d.Identity;
        public Matrix4d ProjectionMatrixD { get { return _ProjectionMatrixD; } private set { _ProjectionMatrixD = value; } }
        public Matrix4d ViewMatrixD { get { return _ViewMatrixD; } private set { _ViewMatrixD = value; } }
        public Matrix4d VP_MatrixD { get { return _VP_MatrixD; } private set { _VP_MatrixD = value; } } // View * Projection (Double precision version)

        public Vector3d CameraPosition;
        public Vector3d LookVector3d;
        public Vector3d UpVector3d;
        public Vector3d NormalVector3d; // Normal to LookDirection and UpDirection

        public FrustumCuller FrustumCuller { get; set; }

        private int FramerateMS { get; set; } // Current framerate, ms/frame
        private bool AnimatingLook { get; set; } = false; // Look Dir or LookAt
        private bool AnimatingTilt { get; set; } = false; // Up Dir or Tilt
        private bool AnimatingLookAt { get; set; } = false;
        private bool AnimatingUDLRFB { get; set; } = false; // Up, Down, Left, Right, Forward, Backward or Move
        private bool AnimatingOrbit { get; set; } = false;
        private bool AnimatingGoNear { get; set; } = false;
        private bool AnimatingCamera
        {
            get { return AnimatingLook | AnimatingTilt | AnimatingLookAt | AnimatingUDLRFB | AnimatingOrbit | AnimatingGoNear; }
        }
        private Reticle Reticle { get; set; }
        public bool ShowReticle { get; set; } = true;
        public SimModel? SimModel { get; set; }
        private Scale Scale { get; set; } // For universe to W coords
        #endregion

        public SimCamera(Scale scale, Vector3d positionPt, Vector3d lookAtPt, Double width, Double height)
        {
            Scale = scale;
            Reticle = new();

            SetAspectRatio(width, height); // Causes Perspective matrix to be generated

            // Calculate values for Camera move scale
            // Theyt are e**SliderValue (SliderValues are 0..20)
            CamMoveKMs = new Double[Properties.Settings.Default.MaxCamMoveScale];
            for (int i = 0; i < CamMoveKMs.Length; i++)
                CamMoveKMs[i] = Math.Exp(i);

            Reset(positionPt, lookAtPt);
        }

        internal void Reset(Vector3d positionPt, Vector3d lookAtPt)
        {
            // Initial conditions
            // UpVector will be in +y direction
            SetCameraPosition(positionPt);

            Vector3d lookVec = lookAtPt - positionPt;
            lookVec.Normalize();

            Vector3d upVec = new(0d, 1d, 0d);
            Vector3d nVec = Vector3d.Cross(lookVec, upVec);
            nVec.Normalize();

            upVec = -Vector3d.Cross(lookVec, nVec);
            upVec.Normalize();

            SetLookVector(lookVec);
            SetUpVector3(upVec);
            SetNormalVector3(nVec);

            // Instantiate here after camera characteristics set
            FrustumCuller = new(this);

            UpdateViewMatrix();
        }

        public void SetAspectRatio(Double width, Double Height)
        {
            ViewWidth = (Single)width;
            ViewHeight = (Single)Height;
            AspectRatio = ViewWidth / ViewHeight; // This will also set the Perspective matrix
        }

        /// <summary>
        /// Animation for camera
        /// WPF animation support is not that great for the kind of animation effects needed/wanted.
        /// so it is implemented here.
        /// This is called as part of the frame calculation process.
        /// </summary>
        /// <param name="ms">ms time of this call as supplied</param>
        /// <param name="framerateMS">Moving average frame rate. A frame is happening on average every framerateMS</param>
        public void AnimateCamera(int ms, int framerateMS)
        {
            bool uVM = false; // Update View Matrix

            // If Keep is on perform the kind of keep specified.
            // Keep is never on while camera animation is active.
            if (SimCamera.KindOfKeep.None != KeepKind)
            {
                KeepOnStation();
                uVM = true;
            }

            if (AnimatingCamera)
            {
                uVM = true;
                FramerateMS = framerateMS; // Rather than a param to each method call

                AnimateOrbit();
                AnimateUDLRFB();
                AnimateTilt();
                AnimateLook();
                AnimateLookAt();
                AnimateGoNear();
            }

            if (uVM)
                UpdateViewMatrix();
        }

        /// <summary>
        /// Render anything visible associated with camera.
        /// </summary>
        internal void Render()
        {
            if (ShowReticle)
                Reticle.Render();
        }
        public void SetCameraPosition(Vector3d position)
        {
            CameraPosition = position;
        }
        public void SetLookVector(Vector3d lookVec)
        {
            LookVector3d = lookVec;
        }
        public void SetUpVector3(Vector3d uVec)
        {
            UpVector3d = uVec;
        }
        public void SetNormalVector3(Vector3d nVec)
        {
            NormalVector3d = nVec;
        }

        /// <summary>
        /// Target body's current position from SimBody reference
        /// </summary>
        /// <param name="position"></param>
        private void BodyPosition(SimBody? sB, out Vector3d position)
        {
            if (sB is null)
                position.X = position.Y = position.Z = 0D;
            else
            {
                position.X = sB.X; position.Y = sB.Y; position.Z = sB.Z;
            }
        }

        /// <summary>
        /// Body's current position from body index;
        /// </summary>
        /// <param name="bodyIndex"></param>
        /// <param name="position"></param>
        private void BodyPosition(int bodyIndex, out Vector3d position)
        {
            SimBody sB = (-1 == OrbitBodyIndex) ? null : SimModel.SimBodyList.BodyList[bodyIndex];
            BodyPosition(sB, out position);
        }

        /// <summary>
        /// Shift camera by the delta between these two points
        /// </summary>
        /// <param name="currPosition"></param>
        /// <param name="prevPosition"></param>
        private void ShiftCamera(Vector3d currPosition, Vector3d prevPosition)
        {
            Vector3d distVector3d = currPosition - prevPosition;
            Double distance = distVector3d.Length;
            if (0D != distance)
                distVector3d.Normalize();

            CameraPosition += (distVector3d * distance);
        }

        /// <summary>
        /// Given current values of CameraPosition, UpDirection and LookDirection construct ViewMatrix.
        /// </summary>
        /// <remarks>
        /// To be called after any series of changes to camera (e.g. position or vectors).
        /// Calculations performed in double precision. Single precision version of VP Matrix is
        /// kept for feeding OpenGL's fragement shader.
        /// </remarks>
        public void UpdateViewMatrix()
        {
            Vector3d eye = new();
            Scale.ScaleU_ToW(ref eye, CameraPosition);

            Vector3d target = eye;
            target.X += LookVector3d.X;
            target.Y += LookVector3d.Y;
            target.Z += LookVector3d.Z;

            Vector3d up;
            up.X = UpVector3d.X;
            up.Y = UpVector3d.Y;
            up.Z = UpVector3d.Z;

            ViewMatrixD = Matrix4d.LookAt(eye, target, up);

            VP_MatrixD = ViewMatrixD * ProjectionMatrixD;

            // Single precision versions for OpenGL
            Matrix4D_toS(ref _ViewMatrixD, ref _ViewMatrix);
            Matrix4D_toS(ref _VP_MatrixD, ref _VP_Matrix);

            FrustumCuller.GenerateFrustum();

            // Test Frustrum culling
            //bool culled = SimCamera.FrustumCuller.SphereCulls(new Vector3d(0D, 0D, -3D), 2D);
        }

        /// <summary>
        /// Matrix4d to Matrix4
        /// </summary>
        /// <param name="matrix4d"></param>
        /// <param name="matrix4"></param>
        private void Matrix4D_toS(ref Matrix4d matrix4d, ref Matrix4 matrix4)
        {
            matrix4.M11 = (Single)matrix4d.M11;
            matrix4.M12 = (Single)matrix4d.M12;
            matrix4.M13 = (Single)matrix4d.M13;
            matrix4.M14 = (Single)matrix4d.M14;
            matrix4.M21 = (Single)matrix4d.M21;
            matrix4.M22 = (Single)matrix4d.M22;
            matrix4.M23 = (Single)matrix4d.M23;
            matrix4.M24 = (Single)matrix4d.M24;
            matrix4.M31 = (Single)matrix4d.M31;
            matrix4.M32 = (Single)matrix4d.M32;
            matrix4.M33 = (Single)matrix4d.M33;
            matrix4.M34 = (Single)matrix4d.M34;
            matrix4.M41 = (Single)matrix4d.M41;
            matrix4.M42 = (Single)matrix4d.M42;
            matrix4.M43 = (Single)matrix4d.M43;
            matrix4.M44 = (Single)matrix4d.M44;
        }

        #region Keep
//        Int32 iCtr = -1;

        /// <summary>
        /// Keeps camera OnStation according to value of KeepKind
        /// </summary>
        private void KeepOnStation()
        {
            SimBody? sB;

            if (-2 == KeepBody)
                return;

            if (-1 == KeepBody)
                sB = null;
            else
                sB = SimModel.SimBodyList.BodyList[KeepBody];

            if (!KeepIsOnStation)
            {
                if (SimCamera.KindOfKeep.LookAtAndDistance == KeepKind)
                {
                    BodyPosition(sB, out KeepPosition); // Location of target when starting KeepOnStation
#if false
                    Double diaAdj = (sB is not null) ? sB.EphemerisDiameter / 2D : 0;
                    System.Diagnostics.Debug.WriteLine("KeepOnStation first call:"
                            + " distance:" + ((CameraPosition - KeepPosition).Length- diaAdj).ToString("#,##0")
                            );
#endif                
                }

                // First call. Ensure a smooth animation from where ever camera is now looking
                // to looking at the KeepBody.
                LookAt(KeepBody);

                KeepIsOnStation = true; // LookAt had caused this to be set this to false. Set it here.
            }
            else
            {
                // Already OnStation, adjust camera to stay OnStation

                // Look direction
                LookAt(sB, 2D * Math.PI);

                // Distance
                if (SimCamera.KindOfKeep.LookAtAndDistance == KeepKind)
                {
                    // Shift camera position by whatever amount the body has shifted.
                    Vector3d currPosition, distVector3d;

                    BodyPosition(sB, out currPosition); // Current location of target

//                    distVector3d = currPosition - KeepPosition;
//                    Double distance = distVector3d.Length;
//                    if (0D != distance)
//                        distVector3d.Normalize();

//                    CameraPosition += (distVector3d * distance);

                    ShiftCamera(currPosition, KeepPosition);
#if false
                    if (0 == ++iCtr % 60)
                    {
                        Double diaAdj = (sB is not null) ? sB.EphemerisDiameter / 2D : 0;
                        System.Diagnostics.Debug.WriteLine("KeepOnStation:"
                                    + " KeepKind:" + KeepKind.ToString()
                                    + " distance:" + ((CameraPosition - currPosition).Length - diaAdj).ToString("#,##0")
                                    );
                    }
#endif
                    KeepPosition = currPosition;
                }
            }
        }
        #endregion

        #region LookAt
        private SimBody? LookAtSimBody { get; set; }
        private static float LookAtRadiansPerFrame = 2.0f * (MathHelper.Pi / 180); // 2.0 degrees
        private KindOfKeep LookAtRetainedKeep { get; set; }

        /// <summary>
        /// Rotate camera to look at a body in the sim
        /// </summary>
        /// <param name="bodyIndex">-1 for origin or body number</param>
        /// 
        public void LookAt(int bodyIndex, bool runAnyway = false)
        {
            // Ignore if command arrives during animation
            if (!runAnyway)
                if (AnimatingCamera)
                    return;

            AnimatingLookAt = true;

            // Disable Keep during this animation
            LookAtRetainedKeep = KeepKind;
            KeepKind = KindOfKeep.None;
            KeepBody = bodyIndex;

            // Where to?
            if (-1 == bodyIndex)
                LookAtSimBody = null;
            else
                LookAtSimBody = SimModel.SimBodyList.BodyList[bodyIndex];
        }

        private void AnimateLookAt()
        {
            if (!AnimatingLookAt)
                return;

            Double angleBetweenLookVectors = LookAt(LookAtSimBody, LookAtRadiansPerFrame);

            if (angleBetweenLookVectors <= LookAtRadiansPerFrame)
            {
                AnimatingLookAt = false; // Animation completed, close enough
                KeepKind = LookAtRetainedKeep; // Resore
            }
        }

        /// <summary>
        /// Update camera vectors to look at LookAtSimBody, rotating a max of maxRadians.
        /// </summary>
        /// <param name="minRotateRadians">Max amount to rotate the vectors</param>
        /// <returns>
        /// Radians between current and target LookVectors -> which may not be the same
        /// angle the vectors are rotated.
        /// </returns>
        private Double LookAt(SimBody? sB, Double minRotateRadians)
        {
            Vector3d lookAtPt;

            BodyPosition(sB, out lookAtPt); // Current location of target

            // Prior to normalization this vector could be really long
            Vector3d newLookVector3d = lookAtPt - CameraPosition;
            newLookVector3d.Normalize();

            Vector3d rotateAboutVector3d = Vector3d.Cross(newLookVector3d, LookVector3d);
            Double angleBetweenLookVectors = Vector3d.CalculateAngle(LookVector3d, newLookVector3d); // Radians

            if (0D != angleBetweenLookVectors) // Not already looking that way?
            {
                Double radiansThisFrame = Math.Min(minRotateRadians, angleBetweenLookVectors);

                OpenTK.Mathematics.Quaterniond q = Util.MakeQuaterniond(rotateAboutVector3d, radiansThisFrame);
                Matrix3d rotationMatrix = Matrix3d.CreateFromQuaternion(q);

                LookVector3d = rotationMatrix * LookVector3d;
                UpVector3d = rotationMatrix * UpVector3d;
                NormalVector3d = rotationMatrix * NormalVector3d;
            }
            return angleBetweenLookVectors;
        }
        #endregion

        #region Orbit
        private CameraOrbitDirections OrbitDirection { get; set; }
        private Single OrbitRadiansGoal { get; set; }
        private Matrix3d OrbitRotationMatrix { get; set; }
        private int OrbitFramesGoal { get; set; }
        private int OrbitFramesSoFar { get; set; }
        private Vector3d OrbitPrevCenterPoint3d { get; set; }
        private KindOfKeep OrbitRetainedKeep { get; set; }

        /// <summary>
        /// Body about which camera orbits
        /// </summary>
        private int OrbitBodyIndex { get; set; } = -1; 

        // .03 degrees/ms. 90 degrees in 3 seconds
        // Adjust this value to change camera orbit rate. ( degrees / seconds / 1000 )
        private static Single OrbitRate { get; } = .03f * (MathHelper.Pi / 180f); // Radians per ms

        /// <summary>
        /// Orbit camera about OrbitAboutPoint3D, by degrees.
        /// Will be a circular orbit. 
        /// For L/R, camera's NormalDirection is tangent to the orbital circle.
        /// For U/D. camera's UpDirection is tangent to the orbital circle
        /// Camera remains looking at same point as before orbit.
        /// All three vectors change.
        /// </summary>
        /// <param name="orbitDirection"></param>
        /// <param name="degrees"></param>
        internal void OrbitCamera(CameraOrbitDirections orbitDirection, Single degrees)
        {
            if (AnimatingCamera)
                return;

            AnimatingOrbit = true;

            OrbitDirection = orbitDirection;
            OrbitRadiansGoal = MathHelper.DegreesToRadians(degrees);
            OrbitFramesSoFar = 0;

            // Disable Keep during this animation
            OrbitRetainedKeep = KeepKind;
            KeepKind = KindOfKeep.None;
        }

        /// <summary>
        /// Implements camera orbit movements
        /// </summary>
        private void AnimateOrbit()
        {
            if (!AnimatingOrbit)
                return;

            // Orbit camera about body's location at beginning of animation. With bodies in motion camera is
            // then offset to body's current location.

            Vector3d aPoint;

            if (0 == OrbitFramesSoFar)
            {
                // As a calculation optimization this assumes the FramerateMS will be nearly constant during 
                // any given movement animation.
                Single totalMS = OrbitRadiansGoal / OrbitRate;
                OrbitFramesGoal = (int)Math.Ceiling(totalMS / FramerateMS);

                Single radiansPerFrame = OrbitRadiansGoal / OrbitFramesGoal;
                OrbitRotationMatrix = Matrix3d.CreateFromQuaternion(CalcOrbitQuaternion(radiansPerFrame));

                // Init conditions for orbit
                BodyPosition(OrbitBodyIndex, out aPoint);
                OrbitPrevCenterPoint3d = aPoint;         // Remember where body was on previous frame
            }

            // Perform rotations of camera vectors and position
            // Orbit is circular about OrbitAboutPoint3D
            // Position rotated about OrbitCenterPoint3d then Transformed to new location.
            //
            //          Orbit tangent to   Rotate about
            // OU, OD         Up             Normal
            // OL, OR       Normal             Up
            //
            // Translate to orbit center, rotate, translate back
            //
            BodyPosition(OrbitBodyIndex, out aPoint);
            Vector3d rotateVec = new(CameraPosition.X - aPoint.X,
                                     CameraPosition.Y - aPoint.Y,
                                     CameraPosition.Z - aPoint.Z);

            Double len = rotateVec.Length;

            rotateVec.Normalize();

            rotateVec *= OrbitRotationMatrix;

            CameraPosition = aPoint + len * rotateVec;

            switch (OrbitDirection)
            {
                case CameraOrbitDirections.OrbitUp:
                case CameraOrbitDirections.OrbitDown:
                    LookVector3d *= OrbitRotationMatrix;
                    UpVector3d *= OrbitRotationMatrix;
                    // NormalDirection is unchanged
                    break;

                case CameraOrbitDirections.OrbitLeft:
                case CameraOrbitDirections.OrbitRight:
                    LookVector3d *= OrbitRotationMatrix;
                    NormalVector3d *= OrbitRotationMatrix;
                    // UpDirection is unchanged
                    break;
            }

            // Offset camera to new position relative to body's current position
            ShiftCamera(aPoint, OrbitPrevCenterPoint3d);

            OrbitPrevCenterPoint3d = aPoint; // Remember

            if (++OrbitFramesSoFar >= OrbitFramesGoal)
            {
                AnimatingOrbit = false; // Animation completed
                KeepKind = OrbitRetainedKeep; // Restore
            }
        }

        private OpenTK.Mathematics.Quaterniond CalcOrbitQuaternion(Single radians)
        {
            Vector3d orbitAboutVector3D;

            if (CameraOrbitDirections.OrbitUp == OrbitDirection || CameraOrbitDirections.OrbitDown == OrbitDirection)
            {
                orbitAboutVector3D = NormalVector3d;

                if (CameraOrbitDirections.OrbitUp == OrbitDirection)
                    radians = -radians;
            }
            else // OL, OR
            {
                orbitAboutVector3D = UpVector3d;

                if (CameraOrbitDirections.OrbitLeft == OrbitDirection)
                    radians = -radians;
            }

            return Util.MakeQuaterniond(orbitAboutVector3D, radians);
        }

        /// <summary>
        /// Tell camera what body to rotate about
        /// </summary>
        /// <param name="bodyIndex">-1 for origin, otherwise index to body in SimBodyList</param>
        public void OrbitAbout(int bodyIndex)
        {
            OrbitBodyIndex = bodyIndex;
        }

        /// <summary>
        /// Check if Keep should be disabled for this orbit. If so disable.
        /// </summary>
        /// <returns>
        /// true if disabled, false otherwise
        /// </returns>
        public bool OrbitTurnOffKeep()
        {
            if (OrbitBodyIndex != KeepBody)
            {
                KeepKind = KindOfKeep.None;
                return true;
            }
            return false;
        }
        #endregion

        #region Movement Up, Down, Left, Right, Forward, Backward, or Move
        private Vector3d UDLRFB_To { get; set; } // Universe coords
        private int UDLRFB_FramesSoFar { get; set; }
        private int ODLRFB_FramesGoal { get; set; }
        private Single ODLRFB_DX_PerFrame { get; set; }
        private Single ODLRFB_DY_PerFrame { get; set; }
        private Single ODLRFB_DZ_PerFrame { get; set; }
        private static Duration UDLRFB_Duration { get; } = new Duration(TimeSpan.FromMilliseconds(500E0));

        /// <summary>
        /// Moves camera L, R, U, D, F, B
        /// None of the camera's vectors change
        /// </summary>
        /// <param name="moveDirection"></param>
        public void Move(CameraMoveDirections moveDirection)
        {
            // Ignore if command arrives during animation
            if (AnimatingCamera)
                return;

            AnimatingUDLRFB = true;

            Double d = CamMoveKMs[Scale.CamMoveAmt];

            // https://math.stackexchange.com/questions/1650877/how-to-find-a-point-which-lies-at-distance-d-on-3d-line-given-a-position-vector

            // Disable Keep during this animation
            LookAtRetainedKeep = KeepKind;
            KeepKind = KindOfKeep.None;
            KeepIsOnStation = false; // Moving camera takes it off station

            Double f;
            UDLRFB_To = CameraPosition;

            switch (moveDirection)
            {
                case CameraMoveDirections.MoveForward:
                case CameraMoveDirections.MoveBackward:
                    // Forward/Backwards on LookDirection unit vector
                    f = (CameraMoveDirections.MoveBackward == moveDirection) ? -d : d;
                    UDLRFB_To += f * LookVector3d;
                    break;

                case CameraMoveDirections.MoveUp:
                case CameraMoveDirections.MoveDown:
                    // Up/Down the UpDirection unit vector
                    f = (CameraMoveDirections.MoveDown == moveDirection) ? -d : d;
                    UDLRFB_To += f * UpVector3d;
                    break;

                case CameraMoveDirections.MoveLeft:
                case CameraMoveDirections.MoveRight:
                    // Left/Right on NormalDirection vectors
                    f = (CameraMoveDirections.MoveRight == moveDirection) ? d : -d;
                    UDLRFB_To += f * NormalVector3d;
                    break;

                default:
                    break;
            }

            UDLRFB_FramesSoFar = 0;
        }

        /// <summary>
        /// Move camera Up, Down, Left, Right, Forward, Backward, or Move
        /// </summary>
        private void AnimateUDLRFB()
        {
            if (!AnimatingUDLRFB)
                return;

            Vector3d currPosn = CameraPosition;

            if (0 == UDLRFB_FramesSoFar)
            {
                // As a calculation optimization this assumes the FramerateMS will be nearly constant during 
                // any given movement animation.
                ODLRFB_FramesGoal = (int)Math.Ceiling((1E3 * UDLRFB_Duration.TimeSpan.TotalSeconds) / FramerateMS);

                // Set up to begin animation
                ODLRFB_DX_PerFrame = (float)(UDLRFB_To.X - currPosn.X) / ODLRFB_FramesGoal;
                ODLRFB_DY_PerFrame = (float)(UDLRFB_To.Y - currPosn.Y) / ODLRFB_FramesGoal;
                ODLRFB_DZ_PerFrame = (float)(UDLRFB_To.Z - currPosn.Z) / ODLRFB_FramesGoal;
            }

            // Calc a frame position
            currPosn.X += ODLRFB_DX_PerFrame;
            currPosn.Y += ODLRFB_DY_PerFrame;
            currPosn.Z += ODLRFB_DZ_PerFrame;

            CameraPosition = currPosn;

            if (++UDLRFB_FramesSoFar >= ODLRFB_FramesGoal)
            {
                AnimatingUDLRFB = false; // Animation completed
                KeepKind = LookAtRetainedKeep; // Resore
            }
        }
        #endregion

        #region GoNear
        private static Double GN_MaxAnimationTimeSecs { get; } = 3E0;  // Max animation will be this many seconds
        private Double GN_NearDistance { get; set; } // What distance from the GoNear body to stop
        private bool StartedGN_LookAtAnimation { get; set; }
        private int GN_FramesGoal { get; set; }
        private int GN_FramesSoFar { get; set; }

        private Vector3d GN_TargetPoint; //
        private SimBody GN_SimBody { get; set; } = null;
        private int GN_BodyIndex { get; set; }
        private KindOfKeep GN_RetainedKeep { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bodyIndex">-1 for origin, otherwise index to body in SimBodyList</param>
        public void GoNear(int bodyIndex)
        {
            // Ignore if command arrives during animation
            if (AnimatingCamera)
                return;

            AnimatingGoNear = true;
            StartedGN_LookAtAnimation = false;
            GN_BodyIndex = bodyIndex;
            GN_FramesSoFar = 0;

            // Where to?
            if (-1 == GN_BodyIndex)
            {
                GN_SimBody = null;
                GN_TargetPoint.X = GN_TargetPoint.Y = GN_TargetPoint.Z = 0D;
                GN_NearDistance = 3E6; // 3M km from origin
            }
            else
            {
                GN_SimBody = SimModel.SimBodyList.BodyList[GN_BodyIndex];
                GN_NearDistance = 3E0 * GN_SimBody.EphemerisDiameter; // Stop N diameters from the body
            }

            // Disable Keep during this animation
            GN_RetainedKeep = KeepKind;
            KeepKind = KindOfKeep.None;
            KeepBody = bodyIndex;
        }

        private void AnimateGoNear()
        {
            if (!AnimatingGoNear)
                return;

            if (!StartedGN_LookAtAnimation)
            {
                // Run through a LookAt animation sequence to get the camera
                // pointed in target body's direction.
                StartedGN_LookAtAnimation = true;
                LookAt(GN_BodyIndex, true /* run anyway */);
            }
            else if (AnimatingLookAt)
            {
                // AnimatingLookAt phase running
            }
            else
            {
                // Animating LookAt phase complete, perform cruise/decelerate phases
                // Bodies are constantlly in motion (except for origin), hence the continual recalculation.
                // Route will be as an arc continually adjusting to the target's position rather than direct line
                // to a stationary target.
                Vector3d targetPosition3D;
                BodyPosition(GN_SimBody, out targetPosition3D); // Current location of target

                Vector3d distVector3d;
                distVector3d.X = targetPosition3D.X - CameraPosition.X;
                distVector3d.Y = targetPosition3D.Y - CameraPosition.Y;
                distVector3d.Z = targetPosition3D.Z - CameraPosition.Z;
                Double currDistToTarget = distVector3d.Length - GN_NearDistance;

                if (currDistToTarget <= GN_NearDistance)
                {
                    AnimatingGoNear = false; // Stop 
                    KeepKind = GN_RetainedKeep; // Resore
                    return;
                }

                if (0 == GN_FramesSoFar)
                {

                    int maxFrames = (int)(GN_MaxAnimationTimeSecs * (1000 / FramerateMS));
                    GN_FramesGoal = (currDistToTarget > OneAU) ? maxFrames : (int)(currDistToTarget / OneAU * maxFrames);
                    if (0 == GN_FramesGoal) // In case really close;
                        GN_FramesGoal = 2;
                }

                Double distThisFrame = currDistToTarget / (GN_FramesGoal - GN_FramesSoFar);

#if false
                System.Diagnostics.Debug.WriteLine("SimCamera..AnimateGoNear "
                    + " currDistToTarget " + currDistToTarget.ToString("0.000000000000E0")
                    + " distThisFrame " + distThisFrame.ToString("0.000000000000E0")
                    + " GN_FramesSoFar " + GN_FramesSoFar.ToString()
                    + " GN_FramesGoal " + GN_FramesGoal.ToString()
                    );
                if (0 == GN_FramesGoal)
                    System.Diagnostics.Debugger.Break();
#endif

                distVector3d.Normalize();
                CameraPosition += (distVector3d * distThisFrame);

                // Ensure camera remains looking at target.
                // Necessary because
                // 1. Round-off errors in calculations cause the camera vectors to drift from target
                // 2. Target body is moving
                // Similar to calculations for LookAt.
                Vector3d newLookVector3d = new(targetPosition3D.X - CameraPosition.X, targetPosition3D.Y - CameraPosition.Y, targetPosition3D.Z - CameraPosition.Z);
                newLookVector3d.Normalize();

                Double angleBetweenLookVectors = Vector3d.CalculateAngle(LookVector3d, newLookVector3d);

                // Smooth out rounding issues looking for vector direction change here
                if (1.74533e-5D < angleBetweenLookVectors) // .001 degrees in radians
                {
#if false
                    System.Diagnostics.Debug.WriteLine("SimCamera..AnimateGoNear "
                        + " angleBetweenLookVectors " + angleBetweenLookVectors.ToString("0.000000000000E0")
                        + " GN_FramesSoFar " + GN_FramesSoFar.ToString()
                        );
#endif

                    Vector3d rotateAboutVector3d = Vector3d.Cross(LookVector3d, newLookVector3d);

                    OpenTK.Mathematics.Quaterniond q = Util.MakeQuaterniond(rotateAboutVector3d, angleBetweenLookVectors);
                    Matrix3d rotationMatrix = Matrix3d.CreateFromQuaternion(q);

                    LookVector3d *= rotationMatrix;
                    UpVector3d *= rotationMatrix;
                    NormalVector3d *= rotationMatrix;
                }

                // Stop animation is complete
                if (++GN_FramesSoFar >= GN_FramesGoal)
                {
                    AnimatingGoNear = false;
                    KeepKind = GN_RetainedKeep; // Resore
                }
            }
        }
        #endregion

        #region Look Direction
        private Matrix3d LookRotationMatrix;
        private int LookFramesGoal { get; set; }
        private int LookFramesSoFar { get; set; }
        private Single LookLR_Theta { get; set; } // radians
        private Single LookUD_Theta { get; set; } // radians
        /// <summary>
        /// Alter look direction of Camera
        /// https://social.msdn.microsoft.com/Forums/en-US/080b45d1-f29b-415b-b1d6-39173185c0f1/rotate-vector-by-a-quaternion?forum=wpf
        /// All three vectors change
        /// </summary>
        /// <param name="lR_Theta">Theta degrees L(-) or R(+)</param>
        /// <param name="uD_Theta">Theta degrees U(+) or D(-)</param>
        public void Look(Single lR_Theta, Single uD_Theta)
        {
            // Ignore if command arrives during animation
            if (AnimatingCamera)
                return;

            AnimatingLook = true;
            LookFramesSoFar = 0;
            LookLR_Theta = MathHelper.DegreesToRadians(lR_Theta);
            LookUD_Theta = MathHelper.DegreesToRadians(uD_Theta);
        }

        /// <summary>
        /// Aminate changing the LookDirection.
        /// Can operate simultaneously in UD and LR (not fully implemented)
        /// </summary>
        private void AnimateLook()
        {
            if (!AnimatingLook)
                return;

            if (0 == LookFramesSoFar)
            {
                // As a calculation optimization this assumes the FramerateMS will be nearly constant during 
                // any given movement animation.

                Single angleBetweenLookVectors = (0D == LookLR_Theta) ? LookUD_Theta : LookLR_Theta;

                if (0f == angleBetweenLookVectors) // No change
                {
                    AnimatingLook = false;
                    return;
                }

                // This calculation assumes, for now, that either LookUD_Theta or LookLR_Theta is 0.
                // Will be necessary to fully calculate new LookDirection, find angle between current
                // and new LookDiretions in order to calc LookFramesGoal.
                Single totalMS = Math.Abs(angleBetweenLookVectors) / OrbitRate;
                LookFramesGoal = (int)Math.Ceiling(totalMS / FramerateMS);

                // This section already set to work against changes to either or both LookUD_Theta or LookLR_Theta
                Single lrRadiansPerFrame = LookLR_Theta / LookFramesGoal;
                Single udRadiansPerFrame = LookUD_Theta / LookFramesGoal;

                // L,R about Camera's UpDirection
                OpenTK.Mathematics.Quaterniond q = Util.MakeQuaterniond(UpVector3d, lrRadiansPerFrame);

                // U,D about camera's NormalDirection vector
                OpenTK.Mathematics.Quaterniond r = Util.MakeQuaterniond(NormalVector3d, udRadiansPerFrame);

                // Combine L,R U,D rotations
                OpenTK.Mathematics.Quaterniond s = q * r;

                LookRotationMatrix = Matrix3d.CreateFromQuaternion(s);
            }

            LookVector3d = LookRotationMatrix * LookVector3d;
            if (0D != LookUD_Theta) // Up will change if UD
                UpVector3d = LookRotationMatrix * UpVector3d;
            if (0D != LookLR_Theta) // Normal will change if LR
                NormalVector3d = LookRotationMatrix * NormalVector3d;

            if (++LookFramesSoFar >= LookFramesGoal)
                AnimatingLook = false; // Animation completed
        }
        #endregion

        #region Tilt/UpVector
        private Matrix3d TiltRotationMatrix; // Getter/Setter do not seem to work here...
        private int TiltFramesGoal { get; set; }
        private int TiltFramesSoFar { get; set; }
        private Single TiltRadians { get; set; }

        /// <summary>
        /// Tilt camera cw or ccw
        /// See https://social.msdn.microsoft.com/Forums/en-US/080b45d1-f29b-415b-b1d6-39173185c0f1/rotate-vector-by-a-quaternion?forum=wpf
        /// for vector rotation technique.
        /// UpDirection and NormalDirection vestors change
        /// </summary>
        /// <param name="tiltDirection"></param>
        public void Tilt(CameraTiltDirections tiltDirection, Single degrees)
        {
            // Ignore if command arrives during animation
            if (AnimatingCamera)
                return;

            TiltRadians = MathHelper.DegreesToRadians((tiltDirection == CameraTiltDirections.TileCounterClockwise) ? -degrees : degrees);
            TiltFramesSoFar = 0;

            AnimatingTilt = true;
        }

        /// <summary>
        /// Camera Tilt or UpDirection
        /// </summary>
        private void AnimateTilt()
        {
            if (!AnimatingTilt)
                return;

            if (0 == TiltFramesSoFar)
            {
                // As a calculation optimization this assumes the FramerateMS will be nearly constant during 
                // any given movement animation.
                //TiltFramesGoal = (int)Math.Ceiling((1E3 * LookUDLR_Duration.TimeSpan.TotalSeconds) / FramerateMS);
                Single totalMS = Math.Abs(TiltRadians / OrbitRate); // degrees can be < 0
                TiltFramesGoal = (int)Math.Ceiling(totalMS / FramerateMS);

                Single radiansPerFrame = TiltRadians / TiltFramesGoal;

                OpenTK.Mathematics.Quaterniond q = Util.MakeQuaterniond(LookVector3d, radiansPerFrame);
                TiltRotationMatrix = Matrix3d.CreateFromQuaternion(q);
            }

            UpVector3d *= TiltRotationMatrix;
            NormalVector3d *= TiltRotationMatrix;

            // Recticle does not change

            if (++TiltFramesSoFar >= TiltFramesGoal)
                AnimatingTilt = false; // Animation completed
        }
        #endregion


    }
}
