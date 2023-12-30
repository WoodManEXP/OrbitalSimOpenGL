using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
        readonly static Double OneAU = 1.49668992E8D; // KM (93M miles);

        // Amount camera moves increses exponentially as the scale increases (0 .. N)
        public Double[] CamMoveKMs { get; }
        private Scale Scale { get; set; } // For universe to W coords
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
                ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(FieldOfView, _aspectRatio,
                                           Scale.ScaleU_ToW(DepthNear), Scale.ScaleU_ToW(DepthFar));
                Reticle.AspectRatio = _aspectRatio; // Tell the Reticle
            }
        }

        // Keep properties
        public enum KindOfKeep
        {
            Nothing
          , LookAt
          , GoNear
        };
        private int KeepBody { get; set; } = -2; // -2 is nothing set
        private KindOfKeep KeepKind { get; set; } = KindOfKeep.Nothing;
        public bool Keep { get; set; } = false;
        private bool RetainedKeep { get; set; }

        public Single ViewWidth { get; set; }
        public Single ViewHeight { get; set; }

        public Matrix4 ProjectionMatrix { get; private set; } = Matrix4.Identity;
        public Matrix4 ViewMatrix { get; private set; } = Matrix4.Identity;
        public Matrix4 VP_Matrix = Matrix4.Identity; // View * Projection

        public Vector3d CameraPosition;
        public Vector3d LookVector3d;
        public Vector3d UpVector3d;
        public Vector3d NormalVector3d; // Normal to LookDirection and UpDirection

        public FrustumCuller FrustumCuller { get; set; }

        private int FramerateMS { get; set; } // Current framerate, ms/frame
        private Boolean AnimatingLook { get; set; } = false; // Look Dir or LookAt
        private Boolean AnimatingTilt { get; set; } = false; // Up Dir or Tilt
        private Boolean AnimatingLookAt { get; set; } = false;
        private Boolean AnimatingUDLRFB { get; set; } = false; // Up, Down, Left, Right, Forward, Backward or Move
        private Boolean AnimatingOrbit { get; set; } = false;
        private Boolean AnimatingGoNear { get; set; } = false;
        private Boolean AnimatingCamera
        {
            get { return AnimatingLook | AnimatingTilt | AnimatingLookAt | AnimatingUDLRFB | AnimatingOrbit | AnimatingGoNear; }
        }
        private Reticle Reticle { get; set; }
        public bool ShowReticle { get; set; } = true;
        public SimModel SimModel { get; set; }
        #endregion

        public SimCamera(SimModel simModel, Vector3d positionPt, Vector3d lookAtPt, Double width, Double height)
        {
            SimModel = simModel;
            Scale = simModel.Scale;

            Reticle = new();

            SetAspectRatio(width, height); // Causes Perspective matrix to be generated

            // Calculate values for Camera move scale
            // Theyt are e**SliderValue (SliderValues are 0..20)
            CamMoveKMs = new Double[Properties.Settings.Default.MaxCamMoveScale];
            for (int i = 0; i < CamMoveKMs.Length; i++)
                CamMoveKMs[i] = Math.Exp(i);

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

            SetLookVector3(lookVec);
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
            // If Keep is true (on) perform the kind of keep specified.
            // Keep is never true while camera animation is active.
            if (Keep)
            {
                if (0 <= KeepBody)  // A body has been selected (and not Origin (-1))
                                    // Start the keep operation
                    switch (KeepKind)
                    {
                        case KindOfKeep.LookAt:
                            LookAt(KeepBody);
                            break;

//                        case KindOfKeep.GoNear:
//                            GoNear(KeepBody);
//                            break;

                        default:
                            break;
                    }
            }

            if (!AnimatingCamera)
                return;

            FramerateMS = framerateMS; // Rather than a param to each method call

            AnimateOrbit();
            AnimateUDLRFB();
            AnimateTilt();
            AnimateLook();
            AnimateLookAt();
            AnimateGoNear();

            UpdateViewMatrix();
        }

        /// <summary>
        /// Render anything visible associated with camera.
        /// </summary>
        internal void Render()
        {
            if (ShowReticle)
                Reticle.Render(this);
        }
        public void SetCameraPosition(Vector3d position)
        {
            CameraPosition = position;
        }
        public void SetLookVector3(Vector3d lookVec)
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
        /// Given current values of CameraPosition, UpDirection and LookDirection construct ViewMatrix.
        /// </summary>
        /// <remarks>To be called after any series of changes to camera (e.g. position or vectors)</remarks>
        public void UpdateViewMatrix()
        {
            Vector3 eye = new();
            Scale.ScaleU_ToW(ref eye, CameraPosition);

            Vector3 target = eye; // new();
            target.X += (float)LookVector3d.X;
            target.Y += (float)LookVector3d.Y;
            target.Z += (float)LookVector3d.Z;

            Vector3 up;
            up.X = (float)UpVector3d.X;
            up.Y = (float)UpVector3d.Y;
            up.Z = (float)UpVector3d.Z;

            ViewMatrix = Matrix4.LookAt(eye, target, up);

            VP_Matrix = ViewMatrix * ProjectionMatrix;

            // Double precision version of VP_Matrix is useful
            //Vector4d row0 = new(VP_Matrix.M11, VP_Matrix.M12, VP_Matrix.M13, VP_Matrix.M14);
            //Vector4d row1 = new(VP_Matrix.M21, VP_Matrix.M22, VP_Matrix.M23, VP_Matrix.M24);
            //Vector4d row2 = new(VP_Matrix.M31, VP_Matrix.M32, VP_Matrix.M33, VP_Matrix.M34);
            //Vector4d row3 = new(VP_Matrix.M41, VP_Matrix.M42, VP_Matrix.M43, VP_Matrix.M44);
            //VP_Matrix4d = new(row0, row1, row2, row3);

            FrustumCuller.GenerateFrustum();

            // Test Frustrum culling
            //bool culled = SimCamera.FrustumCuller.SphereCulls(new Vector3d(0D, 0D, -3D), 2D);
        }

        #region Animate camera LookAt
        private SimBody? LookAtSimBody { get; set; }
        private Vector3d LookAtPoint3d;
        private static float LookAtRadiansPerFrame = 2.0f * (MathHelper.Pi / 180); // 2.0 degrees

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
            RetainedKeep = Keep;
            Keep = false;
            KeepBody = bodyIndex;
            KeepKind = KindOfKeep.LookAt;

            // Where to?
            if (-1 == bodyIndex)
            {
                LookAtSimBody = null;
                LookAtPoint3d.X = LookAtPoint3d.Y = LookAtPoint3d.Z = 0D;
            }
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
                Keep = RetainedKeep; // Resore
            }
        }

        /// <summary>
        /// Update camera vectors to look at LookAtSimBody, rotating a max of maxRadians.
        /// </summary>
        /// <param name="minRotateRadians">MAx amount to rotate the vectors</param>
        /// <returns>
        /// Radians between current and target LookVectors -> which may not be the same
        /// angle the vectors are rotated.
        /// </returns>
        private Double LookAt(SimBody? sB, Double minRotateRadians)
        {
            // New look direction (vector)
            if (null != sB) // Otherwise moving to look at (0,0,0)
            {
                LookAtPoint3d.X = sB.X;
                LookAtPoint3d.Y = sB.Y;
                LookAtPoint3d.Z = sB.Z;
            }
            else
                LookAtPoint3d.X = LookAtPoint3d.Y = LookAtPoint3d.Z = 0D;

            // Prior to normalization this vector could be really long
            Vector3d newLookVector3d = LookAtPoint3d - CameraPosition;
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

        #region Animate camera orbit

        private CameraOrbitDirections OrbitDirection { get; set; }
        private Single OrbitRadiansGoal { get; set; }
        private Matrix3d OrbitRotationMatrix { get; set; }
        private int OrbitFramesGoal { get; set; }
        private int OrbitFramesSoFar { get; set; }
        private Vector3d OrbitCenterPoint3d, OrbitCameraPosition;

        /// <summary>
        /// Body about which camera orbits
        /// </summary>
        int OrbitBodyIndex; // Getter/Setter do not seem to work here...

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
                OrbitCenter(out aPoint);
                OrbitCenterPoint3d = aPoint;            // Orbit about this and translate to new posn as body moves.
                OrbitCameraPosition = CameraPosition;   // Camera handled similarly
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
            Vector3d rotateVec = new(CameraPosition.X - OrbitCenterPoint3d.X,
                                     CameraPosition.Y - OrbitCenterPoint3d.Y,
                                     CameraPosition.Z - OrbitCenterPoint3d.Z);
            Double len = rotateVec.Length;

            rotateVec.Normalize();

            rotateVec *= OrbitRotationMatrix;

            CameraPosition = OrbitCenterPoint3d + len * rotateVec;

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
#if false
            // All orbit-about targets, other than origin, are in motion. So at each frame find location of body
            // about which camera is orbiting.
            OrbitCenter(out aPoint);

            // Offset camera to new position relative to body's current position
            CameraPosition = OrbitCameraPosition + (aPoint - OrbitCenterPoint3d);

            System.Diagnostics.Debug.WriteLine("AnimateOrbit:"
                                            + " body position:" + aPoint.ToString()
                                            + " OrbitCenterPoint3D:" + OrbitCenterPoint3d.ToString()
                                            + " OrbitCameraPosition:" + OrbitCameraPosition.ToString()
                                            + " CameraPosition:" + CameraPosition.ToString()
                                            );
#endif
            if (++OrbitFramesSoFar >= OrbitFramesGoal)
                AnimatingOrbit = false; // Animation completed
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
        /// Current posiion of body being rotated about
        /// </summary>
        /// <param name="position"></param>
        public void OrbitCenter(out Vector3d position)
        {
            if (-1 == OrbitBodyIndex)
                position.X = position.Y = position.Z = 0D;
            else
            {
                position.X = SimModel.SimBodyList.BodyList[OrbitBodyIndex].X;
                position.Y = SimModel.SimBodyList.BodyList[OrbitBodyIndex].Y;
                position.Z = SimModel.SimBodyList.BodyList[OrbitBodyIndex].Z;
            }
        }

        /// <summary>
        /// Tell camera what body to rotate about
        /// </summary>
        /// <param name="bodyIndex">-1 for origin, otherwise index to body in SimBodyList</param>
        public void OrbitAbout(int bodyIndex)
        {
            OrbitBodyIndex = bodyIndex;
        }
        #endregion

        #region Animate camera movement Up, Down, Left, Right, Forward, Backward, or Move
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

            double f;
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
                AnimatingUDLRFB = false; // Animation completed
        }

        #endregion

        #region Animate GoNear
        private static Double GN_MaxAnimationTimeSecs { get; } = 3E0;  // Max animation will be this many seconds
        private Double GN_NearDistance { get; set; } // What distance from the GoNear body to stop
        private bool StartedGN_LookAtAnimation { get; set; }
        private int GN_FramesGoal { get; set; }
        private int GN_FramesSoFar { get; set; }
//        private bool GN_RetainedKeep { get; set; }

        private Vector3d GN_TargetPoint; //
        private SimBody GN_SimBody { get; set; }
        private int GN_BodyIndex { get; set; }
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
                GN_TargetPoint.X = GN_TargetPoint.Y = GN_TargetPoint.Z = 0D;
                GN_NearDistance = 3E6; // 3M km from origin
            }
            else
            {
                GN_SimBody = SimModel.SimBodyList.BodyList[GN_BodyIndex];
                GN_NearDistance = 3E0 * GN_SimBody.EphemerisDiameter; // Stop N diameters from the body
            }

            // Disable Keep during this animation
//            GN_RetainedKeep = Keep;
//            Keep = false;
//            KeepBody = GN_BodyIndex;
        }

        /// <summary>
        /// Target body's current position
        /// </summary>
        /// <param name="position"></param>
        private void GN_TargetPosn(out Vector3d position)
        {
            if (-1 == GN_BodyIndex)
            {
                position.X = position.Y = position.Z = 0D;
            }
            else
            {
                position.X = GN_SimBody.X;
                position.Y = GN_SimBody.Y;
                position.Z = GN_SimBody.Z;
            }
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
//                KeepKind = KindOfKeep.GoNear;

                // Animating LookAt phase complete, perform cruise/decelerate phases
                // Bodies are constantlly in motion (except for origin), hence the continual recalculation.
                // Route will be as an arc continually adjusting to the target's position rather than direct line
                // to a stationary target.
                Vector3d targetPosition3D;
                GN_TargetPosn(out targetPosition3D); // Current location of target

                Vector3d distVector3d;
                distVector3d.X = targetPosition3D.X - CameraPosition.X;
                distVector3d.Y = targetPosition3D.Y - CameraPosition.Y;
                distVector3d.Z = targetPosition3D.Z - CameraPosition.Z;
                Double currDistToTarget = distVector3d.Length - GN_NearDistance;

                if (currDistToTarget <= GN_NearDistance)
                {
                    AnimatingGoNear = false; // Stop 
//                    Keep = GN_RetainedKeep; // Resore
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
//                    Keep = GN_RetainedKeep; // Resore
                }
            }
        }
        #endregion

        #region Animate camera Look Direction
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

        #region Animate camera Tilt/UpVector
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
