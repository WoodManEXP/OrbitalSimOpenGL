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
        readonly static Double OneAU = 1.49668992D + 8; // KM (93M miles);

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
            if (!AnimatingCamera)
                return;

            FramerateMS = framerateMS; // Rather than a param to each method call

            //AnimateOrbit();
            AnimateUDLRFB();
            AnimateTilt();
            AnimateLook();
            AnimateLookAt();
            AnimateGoNear();

            UpdateViewMatrix();

            // Test Frustrum culling
            //bool culled = SimCamera.FrustumCuller.SphereCulls(new Vector3d(0D, 0D, -3D), 2D);
        }

        /// <summary>
        /// Render anything visible associated with camera.
        /// </summary>
        /// <param name="timeSpan"></param>
        internal void Render(TimeSpan timeSpan)
        {
            Reticle.Render(timeSpan, this);
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

            FrustumCuller.GenerateFrustum();

            //eye = new(0f, 0f, 7f);
            //target = new(0f, 0f, 0f);
            //up = new(0f, 1f, 0f);
            //ViewMatrix = Matrix4.LookAt(eye, target, up);
        }

        #region Animate camera LookAt
        private SimBody? LookAtSimBody { get; set; }
        private Vector3d LookAtPoint3d;
        private static float LookAtRadiansPerFrame = 2.0f * (MathHelper.Pi / 180); // 2.0 degrees
        //private Matrix3D LookAtRotationMatrix; // Getter/Setter do not seem to work here...

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

            // New look direction (vector)
            if (null != LookAtSimBody) // Otherwise moving to look at (0,0,0)
            {
                LookAtPoint3d.X = LookAtSimBody.X;
                LookAtPoint3d.Y = LookAtSimBody.Y;
                LookAtPoint3d.Z = LookAtSimBody.Z;
            }

            // Prior to normalization this vector could be really long
            Vector3d newLookVector3d = LookAtPoint3d - CameraPosition;
            newLookVector3d.Normalize();

            Vector3d rotateAboutVector3d = Vector3d.Cross(newLookVector3d, LookVector3d);
            Double angleBetweenLookVectors = Vector3d.CalculateAngle(LookVector3d, newLookVector3d); // Radians

            if (0D == angleBetweenLookVectors) // Already looking that way?
            {
                AnimatingLookAt = false;
                return;
            }

            Double radiansThisFrame = Math.Min(LookAtRadiansPerFrame, angleBetweenLookVectors);

            OpenTK.Mathematics.Quaterniond q = Util.MakeQuaterniond(rotateAboutVector3d, radiansThisFrame);
            Matrix3d rotationMatrix = Matrix3d.CreateFromQuaternion(q);

            LookVector3d = rotationMatrix * LookVector3d;
            UpVector3d = rotationMatrix * UpVector3d;
            NormalVector3d = rotationMatrix * NormalVector3d;

            if (angleBetweenLookVectors <= LookAtRadiansPerFrame)
                AnimatingLookAt = false; // Animation completed, close enough
        }
        #endregion

        #region Animate camera orbit

        /// <summary>
        /// Body about which camera orbits
        /// </summary>
        int OrbitBodyIndex; // Getter/Setter do not seem to work here...

        // .03 degrees/ms. 90 degrees in 3 seconds
        // Adjust this value to change camera orbit rate. ( degrees / seconds / 1000 )
        private static Single OrbitRate { get; } = .03f; // Degrees per ms

        /// <summary>
        /// Orbit camera about OrbitAboutPoint3D, by degrees.
        /// Will be a circular orbit. 
        /// For L/R, camera's NormalDirection is tangest to the orbital circle.
        /// For U/D. camera's UpDirection is tangent to the orbital circle
        /// Camera remains looking at same point as before orbit.
        /// All three vectors change.
        /// Animation position changes for Camera orbit are handled here rather than by the WPF animation stuff.
        /// </summary>
        /// <param name="orbitDirection"></param>
        /// <param name="degrees"></param>
        internal void OrbitCamera(CameraOrbitDirections orbitDirection, double degrees)
        {
            if (AnimatingCamera)
                return;

            AnimatingOrbit = true;

            //            OrbitDirection = orbitDirection;
            //            OrbitDegreesGoal = degrees;
            //            OrbitFramesSoFar = 0;
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
        private static Double GN_MaxAnimationTimeMS { get; } = 3000E0;  // Max animation will be this many ms
        private static Double GN_MaxDecelTimeMS { get; } = 2000E0;      // Max deceleration time will be this many ms
        private Double GN_NearDistance { get; set; } // What distance from the GoNear body to stop
        private Double GN_CruiseStopDistance { get; set; }
        private bool StartedGN_LookAtAnimation { get; set; }
        private int GN_FramesGoal { get; set; }
        private int GN_CruiseFramesGoal { get; set; }
        private int GN_DecelFramesGoal { get; set; }
        private int GN_FramesSoFar { get; set; }
        private Double GN_Velocity { get; set; } // km/frame

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
                GN_CruiseStopDistance = 0D;
            }
            else
            {
                GN_SimBody = SimModel.SimBodyList.BodyList[GN_BodyIndex];
                GN_TargetPoint.X = GN_SimBody.X;
                GN_TargetPoint.Y = GN_SimBody.Y;
                GN_TargetPoint.Z = GN_SimBody.Z;
                GN_NearDistance = 15E-1 * GN_SimBody.EphemerisDiameter; // Stop N diameters from the body
                GN_CruiseStopDistance = 6E1 * GN_SimBody.EphemerisDiameter; // Cruise phase stops N diameters from the body
            }
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

                Double distThisFrame = 0D;

                if (0 == GN_FramesSoFar)
                {
                    // If camera is already within GN_NearDistance, nothing to do
                    if (currDistToTarget <= GN_NearDistance)
                    {
                        AnimatingGoNear = false; // Stop 
                        return;
                    }

                    // As a calculation optimization this assumes the FramerateMS will be nearly constant during 
                    // any given movement animation.

                    // First timne thru, calculate target length of animation (ms) and deceleration (ms)
                    // 1 .. GN_MaxAnimationTime MS up to distance of 1 AU
                    // Up to OneAU is a proportion of GN_MaxAnimationTime. All over OneAU are set to max.
                    Double animationTime = (currDistToTarget <= OneAU) ? Math.Max(1.5E3, currDistToTarget / OneAU * GN_MaxAnimationTimeMS) : GN_MaxAnimationTimeMS;
                    Double decelerationTime = (currDistToTarget <= OneAU) ? currDistToTarget / OneAU * GN_MaxDecelTimeMS : GN_MaxDecelTimeMS;

                    GN_FramesGoal = (int)Math.Ceiling(animationTime / FramerateMS);
                    GN_DecelFramesGoal = (int)Math.Ceiling(decelerationTime / FramerateMS);
                    GN_CruiseFramesGoal = GN_FramesGoal - GN_DecelFramesGoal;
                }

                if (GN_FramesSoFar < GN_CruiseFramesGoal) // Cruise phase
                {
                    // Because target body is moving, continually adjust distPerFrame during cruise phase to arrive at
                    // deceleration point at scheduled time.
                    // Cruise persists at this, or nearly this. Deceleration begins at last cruise velocity.
                    int cruiseFramesRemaining = GN_CruiseFramesGoal - GN_FramesSoFar;
                    Double remainingCruiseDist = currDistToTarget - GN_CruiseStopDistance;

                    GN_Velocity = remainingCruiseDist / cruiseFramesRemaining;

                    distThisFrame = GN_Velocity;

                    //                    System.Diagnostics.Debug.WriteLine("AnimateGoNear: cruise"
                    //                        + " FramerateMS:" + FramerateMS.ToString("N0")
                    //                        + " cruiseV:" + GN_Velocity.ToString("N0")
                    //                        + " remainingCruiseDist:" + remainingCruiseDist.ToString("N0")
                    //                        + " currDistToTarget:" + currDistToTarget.ToString("N0")
                    //                        + " distThisFrame:" + distThisFrame.ToString("N0")
                    //                        + " cruiseFrameRemaining:" + cruiseFramesRemaining.ToString("N0")
                    //                    );
                }
                else
                {
                    // Deceleration phase
                    // Because target body is moving, continually adjust deceleration rate across frames to arrive at
                    // target point by the last frame.
                    int dcelFramesRemaining = GN_FramesGoal - GN_FramesSoFar;

                    Double decelV = (2D * currDistToTarget) / dcelFramesRemaining;
                    Double acceleration = -currDistToTarget / dcelFramesRemaining;

                    distThisFrame = decelV + acceleration / 2d;

                    //                    System.Diagnostics.Debug.WriteLine("AnimateGoNear: decel"
                    //                        + " decelV:" + decelV.ToString("N0")
                    //                        + " acceleration:" + acceleration.ToString("N0")
                    //                        + " currDistToTarget: " + currDistToTarget.ToString("N0")
                    //                        + " distThisFrame:" + distThisFrame.ToString("N0")
                    //                        + " dcelFramesRemaining:" + dcelFramesRemaining.ToString("N0")
                    //                        + " GN_FramesGoal:" + GN_FramesGoal.ToString("N0")
                    //                        + " GN_FramesSoFar:" + GN_FramesSoFar.ToString("N0")
                    //                    );
                }

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
                    Vector3d rotateAboutVector3d = Vector3d.Cross(LookVector3d, newLookVector3d);

                    OpenTK.Mathematics.Quaterniond q = Util.MakeQuaterniond(rotateAboutVector3d, angleBetweenLookVectors);
                    Matrix3d rotationMatrix = Matrix3d.CreateFromQuaternion(q);

                    LookVector3d = rotationMatrix * LookVector3d;
                    UpVector3d = rotationMatrix * UpVector3d;
                    NormalVector3d = rotationMatrix * NormalVector3d;
                }

                // Stop animation is complete
                if (++GN_FramesSoFar >= GN_FramesGoal)
                    AnimatingGoNear = false;
            }
        }
        #endregion

        #region Animate camera Look Direction
        private Matrix3d LookRotationMatrix;
        private int LookFramesGoal { get; set; }
        private int LookFramesSoFar { get; set; }
        private Single LookLR_Theta { get; set; }
        private Single LookUD_Theta { get; set; }
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
            LookLR_Theta = lR_Theta;
            LookUD_Theta = uD_Theta;
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
                Single lrRadiansPerFrame = MathHelper.DegreesToRadians(LookLR_Theta / LookFramesGoal);
                Single udRadiansPerFrame = MathHelper.DegreesToRadians(LookUD_Theta / LookFramesGoal);

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
        private Single TiltDegrees { get; set; }

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

            TiltDegrees = (tiltDirection == CameraTiltDirections.TileCounterClockwise) ? -degrees : degrees;
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
                Single totalMS = Math.Abs(TiltDegrees / OrbitRate); // degrees can be < 0
                TiltFramesGoal = (int)Math.Ceiling(totalMS / FramerateMS);

                Single radiansPerFrame = MathHelper.DegreesToRadians(TiltDegrees / TiltFramesGoal);

                OpenTK.Mathematics.Quaterniond q = Util.MakeQuaterniond(LookVector3d, radiansPerFrame);
                TiltRotationMatrix = Matrix3d.CreateFromQuaternion(q);
            }

            UpVector3d = TiltRotationMatrix * UpVector3d;
            NormalVector3d = TiltRotationMatrix * NormalVector3d;

            // Recticle does not change

            if (++TiltFramesSoFar >= TiltFramesGoal)
                AnimatingTilt = false; // Animation completed
        }
        #endregion
    }
}
