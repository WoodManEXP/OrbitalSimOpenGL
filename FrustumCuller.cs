using OpenTK.Mathematics;
using System;
using System.Windows.Media.Animation;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Used to cull items from scene prior to insertion into GPU pipeline
    /// </summary>
    /// <remarks>
    /// Operates in Universe coordinates, Doubles
    /// </remarks>
    public class FrustumCuller
    {
        #region Properties
        SimCamera SimCamera { get; set; }

        // Hang on to these camera properties to detect changes in order to avoid
        // unnecessary frustum recalculations.
        Vector3d CameraPosition { get; set; }
        Vector3d LookVector3d { get; set; }
        Single FieldOfView { get; set; } = 0f; // Initial condition to detect first time
        Single AspectRatio { get; set; }
        Double DepthNear { get; set; }
        Double DepthFar { get; set; }

        // For double precision usage
        Vector3d TopFaceNormalD;
        Vector3d BottomFaceNormalD;
        Vector3d RightFaceNormalD;
        Vector3d LeftFaceNormalD;
        Vector3d NearFaceNormalD;
        Vector3d FarFaceNormalD;

        Vector3d CenterPointNearD;   // Serves also as point on near plane
        Vector3d CenterPointFarD;    // Serves also as point on far plane

        // For single precision usage
//        Vector3 TopFaceNormal;
//        Vector3 BottomFaceNormal;
//        Vector3 RightFaceNormal;
//        Vector3 LeftFaceNormal;
//        Vector3 NearFaceNormal;
//        Vector3 FarFaceNormal;

//        Vector3 CenterPointNear;   // Serves also as point on near plane
//        Vector3 CenterPointFar;    // Serves also as point on far plane
        #endregion

        public FrustumCuller(SimCamera simCamera)
        {
            SimCamera = simCamera;
        }

        /// <summary>
        /// Given current camera settings, generate frustum normals and points on near and far planes
        /// that are useful for frustum culling.
        /// </summary>
        /// <remarks>
        /// This is to be called on each rendering cycle, before 
        /// culling/sending stuff to the rendering pipeline.
        /// </remarks>
        /// <param name="simCamera"></param>
        public void GenerateFrustum()
        {
            if (CameraHasChanged())
            {
                // Record relevant properties
                CameraPosition = SimCamera.CameraPosition;
                LookVector3d = SimCamera.LookVector3d;
                FieldOfView = SimCamera.FieldOfView;
                AspectRatio = SimCamera.AspectRatio;
                DepthNear = SimCamera.DepthNear;
                DepthFar = SimCamera.DepthFar;

                NearFaceNormalD = -LookVector3d;
                FarFaceNormalD = LookVector3d;

                // Centerpoint on near and far planes
                CenterPointFarD = CameraPosition + FarFaceNormalD * DepthFar;
                CenterPointNearD = CameraPosition + NearFaceNormalD * DepthNear;

//                CenterPointFar = (Vector3)CenterPointFarD;
//                CenterPointNear = (Vector3)CenterPointNearD;

                Double halfVSide = DepthFar * MathHelper.Tan(FieldOfView * .5f);
                Double halfHSide = halfVSide * AspectRatio;

                // Normal vector along right face
                Vector3d v = (CenterPointFarD + halfHSide * SimCamera.NormalVector3d) - CameraPosition;
                RightFaceNormalD = Vector3d.Cross(v, SimCamera.UpVector3d);
                RightFaceNormalD.Normalize();
//                RightFaceNormal = (Vector3)RightFaceNormalD;

                // Normal vector along left face
                v = (CenterPointFarD - halfHSide * SimCamera.NormalVector3d) - CameraPosition;
                LeftFaceNormalD = Vector3d.Cross(SimCamera.UpVector3d, v);
                LeftFaceNormalD.Normalize();
//                LeftFaceNormal = (Vector3)LeftFaceNormalD;

                // Normal vector along top face
                v = (CenterPointFarD + halfVSide * SimCamera.UpVector3d) - CameraPosition;
                TopFaceNormalD = Vector3d.Cross(SimCamera.NormalVector3d, v);
                TopFaceNormalD.Normalize();
//                TopFaceNormal = (Vector3)TopFaceNormalD;

                // Normal vector along bottom face
                v = (CenterPointFarD - halfVSide * SimCamera.UpVector3d) - CameraPosition;
                BottomFaceNormalD = Vector3d.Cross(v, SimCamera.NormalVector3d);
                BottomFaceNormalD.Normalize();
//                BottomFaceNormal = (Vector3)BottomFaceNormalD;
            }
        }

        /// <summary>
        /// Have relevant camera properties changed since last checked
        /// </summary>
        /// <returns></returns>
        private bool CameraHasChanged()
        {
            if (CameraPosition != SimCamera.CameraPosition)
                return true;
            if (LookVector3d != SimCamera.LookVector3d)
                return true;
            if (FieldOfView != SimCamera.FieldOfView)
                return true;
            if (AspectRatio != SimCamera.AspectRatio)
                return true;
            if (DepthNear != SimCamera.DepthNear)
                return true;
            if (DepthFar != SimCamera.DepthFar)
                return true;

            return false;
        }

        /// <summary>
        /// Returns true if the sphere is completely outside the frustrum
        /// </summary>
        /// <param name="center">Of shere is U coords</param>
        /// <param name="diameter">Of shere in U coords</param>
        /// <returns>True if sphere is culled, false otherwise</returns>
        public Boolean SphereCulls(ref Vector3d center, Double diameter)
        {
            // https://community.khronos.org/t/clipping-test-for-quad-visibility/49368/4
            // https://learnopengl.com/Guest-Articles/2021/Scene/Frustum-Culling
            // https://mathinsight.org/distance_point_plane#:~:text=The%20length%20of%20the%20gray,dot%20product%20v%E2%8B%85n.

            // Vec between sphere center and a point on the r, l, t, b frustum planes.
            // Each of those planes intersects the camera position.
            Vector3d v = center - SimCamera.CameraPosition;

            // Right face
            if (diameter < Vector3d.Dot(v, RightFaceNormalD))
                return true;

            // Left face
            if (diameter < Vector3d.Dot(v, LeftFaceNormalD))
                return true;

            // Top face
            if (diameter < Vector3d.Dot(v, TopFaceNormalD))
                return true;

            // Bottom face
            if (diameter < Vector3d.Dot(v, BottomFaceNormalD))
                return true;

            // Near face
            v = center - CenterPointNearD;
            if (diameter < Vector3d.Dot(v, NearFaceNormalD))
                return true;

            // Far face
            v = center - CenterPointFarD;
            if (diameter < Vector3d.Dot(v, FarFaceNormalD))
                return true;

            return false;
        }


        public Boolean PointCulls(ref Vector3 point)
        {

            return false;
        }
    }
}
