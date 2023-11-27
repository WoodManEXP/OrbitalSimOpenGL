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
        float FieldOfView { get; set; } = 0f; // Initial condition to detect first time
        float AspectRatio { get; set; }
        Double DepthNear { get; set; }
        Double DepthFar { get; set; }

        Vector3d TopFaceNormal;
        Vector3d BottomFaceNormal;
        Vector3d RightFaceNormal;
        Vector3d LeftFaceNormal;
        Vector3d NearFaceNormal;
        Vector3d FarFaceNormal;

        Vector3d CenterPointNear;   // Serves also as point on near plane
        Vector3d CenterPointFar;    // Serves also as point on far plane
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

                NearFaceNormal = -LookVector3d;
                FarFaceNormal = LookVector3d;

                // Centerpoint on near and far planes
                CenterPointFar = CameraPosition + FarFaceNormal * DepthFar;
                CenterPointNear = CameraPosition + NearFaceNormal * DepthNear;

                Double halfVSide = DepthFar * MathHelper.Tan(FieldOfView * .5f);
                Double halfHSide = halfVSide * AspectRatio;

                // Normal vector along right face
                Vector3d v = (CenterPointFar + halfHSide * SimCamera.NormalVector3d) - CameraPosition;
                RightFaceNormal = Vector3d.Cross(v, SimCamera.UpVector3d);
                RightFaceNormal.Normalize();

                // Normal vector along left face
                v = (CenterPointFar - halfHSide * SimCamera.NormalVector3d) - CameraPosition;
                LeftFaceNormal = Vector3d.Cross(SimCamera.UpVector3d, v);
                LeftFaceNormal.Normalize();

                // Normal vector along face
                v = (CenterPointFar + halfVSide * SimCamera.UpVector3d) - CameraPosition;
                TopFaceNormal = Vector3d.Cross(SimCamera.NormalVector3d, v);
                TopFaceNormal.Normalize();

                // Normal vector along face
                v = (CenterPointFar - halfVSide * SimCamera.UpVector3d) - CameraPosition;
                BottomFaceNormal = Vector3d.Cross(v, SimCamera.NormalVector3d);
                BottomFaceNormal.Normalize();
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
        /// <param name="radius">Of shere in U coords</param>
        /// <returns></returns>
        public Boolean SphereCulls(Vector3d center, Double radius)
        {
            // https://community.khronos.org/t/clipping-test-for-quad-visibility/49368/4
            // https://learnopengl.com/Guest-Articles/2021/Scene/Frustum-Culling
            // https://mathinsight.org/distance_point_plane#:~:text=The%20length%20of%20the%20gray,dot%20product%20v%E2%8B%85n.

            Double d;
            Vector3d v;

            // Vec between sphere center and a point on the r, l, t, b frustum planes.
            // Each of those planes intersects the camera position.
            v = center - SimCamera.CameraPosition;

            // Right face
            if (radius < (d = Vector3d.Dot(v, RightFaceNormal)))
                return true;

            // Left face
            if (radius < (d = Vector3d.Dot(v, LeftFaceNormal)))
                return true;

            // Top face
            if (radius < (d = Vector3d.Dot(v, TopFaceNormal)))
                return true;

            // Bottom face
            if (radius < (d = Vector3d.Dot(v, BottomFaceNormal)))
                return true;

            // Near face
            v = center - CenterPointNear;
            if (radius < (d = Vector3d.Dot(v, NearFaceNormal)))
                return true;

            // Far face
            v = center - CenterPointFar;
            if (radius < (d = Vector3d.Dot(v, FarFaceNormal)))
                return true;

            return false;
        }
    }
}
