using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Each SimBody reoresents a body in the sumulation
    /// </summary>
    public class SimBody
    {
        #region Properties

        Scale Scale { get; set; }

        // Current settings, U coords
        public Double X { get; set; } // km
        public Double Y { get; set; } // km
        public Double Z { get; set; } // km
        public Double VX { get; set; } // km/s
        public Double VY { get; set; } // km/s
        public Double VZ { get; set; } // km/s

        // Other settings from Ephemeris reading
        public Double LT { get; set; }
        public Double RG { get; set; }
        public Double RR { get; set; }
        public Double EphemerisDiameter { get; set; } // U coords
        public Double UseD { get; set; } // U coords, gets adjusted for visability
        Double HalfEphemerisD { get; set; }
        public Double Mass { get; set; }
        public Double GM { get; set; }
        public string ID { get; private set; }
        public string Name { get; private set; }

        // For trace path drawing
        public Vector3D LastTraceVector3D;      // Watching curvature rate for drawing path "lines"
        public Point3D LastTracePoint3D;        // U coords

        Color4 BodyColor { get; set; }

        private String AppDataFolder { get; set; }

        #endregion
        public SimBody(EphemerisBody ephemerisBody, String appDataFolder)
        {
            double dVal;

            AppDataFolder = appDataFolder;

            // Save ephemeris values into current settings (universe coords)
            X = double.TryParse(ephemerisBody.X_Str, out dVal) ? dVal : -1D;
            Y = double.TryParse(ephemerisBody.Y_Str, out dVal) ? dVal : -1D;
            Z = double.TryParse(ephemerisBody.Z_Str, out dVal) ? dVal : -1D;
            VX = double.TryParse(ephemerisBody.VX_Str, out dVal) ? dVal : -1D;
            VY = double.TryParse(ephemerisBody.VY_Str, out dVal) ? dVal : -1D;
            VZ = double.TryParse(ephemerisBody.VZ_Str, out dVal) ? dVal : -1D;
            LT = double.TryParse(ephemerisBody.LT_Str, out dVal) ? dVal : -1D;
            //LT = double.TryParse(ephemerisBody.LT_Str, out dVal) ? dVal : -1D;
            RG = double.TryParse(ephemerisBody.RG_Str, out dVal) ? dVal : -1D;
            RR = double.TryParse(ephemerisBody.RR_Str, out dVal) ? dVal : -1D;
            EphemerisDiameter = double.TryParse(ephemerisBody.DiameterStr, out dVal) ? dVal : -1D;
            HalfEphemerisD = EphemerisDiameter / 2D;
            Mass = double.TryParse(ephemerisBody.MassStr, out dVal) ? dVal : -1D;
            GM = double.TryParse(ephemerisBody.GM_Str, out dVal) ? dVal : -1D;

            // ID and Name are also useful
            ID = ephemerisBody.ID;
            Name = ephemerisBody.Name;

            LastTracePoint3D.X = X;
            LastTracePoint3D.Y = Y;
            LastTracePoint3D.Z = Z;
        }

        public void InitBody(Scale scale)
        {
            Scale = scale;

            BodyColor = Name switch
            {
                "Sun" => Color4.Yellow,
                "Mercury" => Color4.SlateGray,
                "Venus" => Color4.White,
                "Moon" => Color4.SlateGray,
                "Earth" => Color4.Blue,
                "Phobos" => Color4.Green,
                "Deimos" => Color4.Green,
                "Mars" => Color4.DarkRed,
                "Jupiter" => Color4.Beige,
                "Saturn" => Color4.Beige,
                "Uranus" => Color4.LightGreen,
                "Neptune" => Color4.SlateBlue,
                "Pluto" => Color4.Tan,
                _ => Color4.DarkOrange
            };
        }

        /// <summary>
        /// Render body using the shared sphere
        /// </summary>
        /// <param name="indicesLength"></param>
        /// <param name="bodyColorUniform">OpenGL shader uniforn number for color</param>
        /// <param name="mvp_Uniform">OpenGL shader uniforn number for mvp matrix4</param>
        /// <param name="vp">View Projection matrix</param>
        /// <param name="locationMatrix4">Will be used/modified to set loc for this body</param>
        /// <param name="sizeMatrix4">Will be used/modified to set size for this body</param>
        /// <remarks>
        /// Assumes the shared unit sphere is what's currently loaded in OpenGL ArrayBuffer. So this
        /// is applying a set of transformations to that sphere to render this body.
        /// </remarks>
        internal void Render(int indicesLength, int bodyColorUniform, int mvp_Uniform, ref Matrix4 vp, ref Matrix4 locationMatrix4, ref Matrix4 sizeMatrix4)
        {
            locationMatrix4.M41 = Scale.ScaleU_ToW(X); // X
            locationMatrix4.M42 = Scale.ScaleU_ToW(Y); // Y
            locationMatrix4.M43 = Scale.ScaleU_ToW(Z); // Z

            sizeMatrix4.M11 = sizeMatrix4.M22 = sizeMatrix4.M33 = Scale.ScaleU_ToW(UseD);

            Matrix4 mvp = sizeMatrix4 * locationMatrix4 * vp;

            GL.Uniform4(bodyColorUniform, BodyColor);
            GL.UniformMatrix4(mvp_Uniform, false, ref mvp);

            GL.DrawElements(PrimitiveType.Triangles, indicesLength, DrawElementsType.UnsignedShort, 0);
        }

        /// <summary>
        /// Get curreent position of a body (x, y, z)
        /// </summary>
        /// <returns>Point3D in universe coords</returns>
        public void GetPosition(ref Vector3d position)
        {
            position.X = X;
            position.Y = Y;
            position.Z = Z;
        }

        /// <summary>
        /// If body has become too small in 3D to 2D projection adjust its size so it remains visible
        /// </summary>
        /// <param name="vp_Matrix"></param>
        /// <param name="viewWidth">in pixels</param>
        /// <param name="halfNorm"></param>
        /// <param name="minSize"></param>
        /// <param name="minSizeSquared"></param>
        /// <remarks>This only works on spheres</remarks>
        public void KeepVisible(ref Matrix4 vp_Matrix, Single viewWidth, ref Vector3d halfNorm, int minSize, int minSizeSquared)
        {
            //TestProjetion();

            // Calc Point3D coordinates for both ends of a vector parallel to camera's UpDirection,
            // centered at sB.X, sB.Y, sB.Z with length = sB's diameter. These represents widest
            // points of sphere as seen by the camera.
            Vector3d oneSideUniv = new(X + HalfEphemerisD * halfNorm.X, Y + HalfEphemerisD * halfNorm.Y, Z + HalfEphemerisD * halfNorm.Z);
            Vector3d otherSideUniv = new(X - HalfEphemerisD * halfNorm.X, Y - HalfEphemerisD * halfNorm.Y, Z - HalfEphemerisD * halfNorm.Z);

            // U coords to W coords
            Scale.ScaleVector3D(ref oneSideUniv);
            Scale.ScaleVector3D(ref otherSideUniv);

            // Make 3D to Homogenous 4D
            Vector4 oneSideV4 = new((Vector3)oneSideUniv, 1f);
            Vector4 otherSideV4 = new((Vector3)otherSideUniv, 1f);

            // To clip space (through View and projection matrices)
            Vector4 oneSideClip = oneSideV4 * vp_Matrix;
            Vector4 otherSideClip = otherSideV4 * vp_Matrix;

            // To clip space
            oneSideClip /= oneSideClip.W;
            otherSideClip /= otherSideClip.W;
            oneSideClip *= viewWidth;
            otherSideClip *= viewWidth;

            // Distance between the two points in clip space (-1 .. 1)
            Single dX = oneSideClip.X - otherSideClip.X;
            Single dY = oneSideClip.Y - otherSideClip.Y;
            Single distSquared = dX * dX + dY * dY;

            if (1E-3 >= distSquared)
            {
                // Body so small it is lost in single precision. Jump to double precision.

                // Make 3D to Homogenous 4D
                Vector4d oneSideV4D = new(oneSideUniv, 1d);
                Vector4d otherSideV4D = new(otherSideUniv, 1d);

                Vector4d row0 = new(vp_Matrix.M11, vp_Matrix.M12, vp_Matrix.M13, vp_Matrix.M14);
                Vector4d row1 = new(vp_Matrix.M21, vp_Matrix.M22, vp_Matrix.M23, vp_Matrix.M24);
                Vector4d row2 = new(vp_Matrix.M31, vp_Matrix.M32, vp_Matrix.M33, vp_Matrix.M34);
                Vector4d row3 = new(vp_Matrix.M41, vp_Matrix.M42, vp_Matrix.M43, vp_Matrix.M44);
                Matrix4d vp_Matrix4d = new(row0, row1, row2, row3);

                // To clip space (through View and projection matrices)
                Vector4d oneSideClipD = oneSideV4D * vp_Matrix4d;
                Vector4d otherSideClipD = otherSideV4D * vp_Matrix4d;

                // To pixels
                oneSideClipD /= oneSideClipD.W;
                otherSideClipD /= otherSideClipD.W;
                oneSideClipD *= viewWidth;
                otherSideClipD *= viewWidth;

                // Distance between the two points in clip space (-1 .. 1)
                Double dXD = oneSideClipD.X - otherSideClipD.X;
                Double dYD = oneSideClipD.Y - otherSideClipD.Y;
                Double distSquaredD = dXD * dXD + dYD * dYD;

                // Change the body's diameter such that distSquared will transform to minSizeSquared
                UseD = (minSize * EphemerisDiameter) / Math.Sqrt(distSquaredD);
            }
            else
            {
                if (distSquared < minSizeSquared)
                    // Change the body's diameter such that distSquared will transform to minSizeSquared
                    UseD = (minSize * EphemerisDiameter) / Math.Sqrt(distSquared);
                else
                    // Not too small, keep the diameter as the original value
                    UseD = EphemerisDiameter;
            }
        }

#if false
        void TestProjetion()
        {
            // View Matrix
            Vector3 eye = new(0f, 0f, 0f);
            Vector3 target = new(0f, 0f, -1f);
            Vector3 up = new(0f, 1f, 0f);
            Matrix4 viewMatrix = Matrix4.LookAt(eye, target, up);

            // Projection/clip matrix
            Single fov = 60.0f * (MathHelper.Pi / 180f);
            Single ar = 1f;
            Single depthNear = .01f, depthFar = 12f;
            Matrix4 projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(fov, ar, depthNear, depthFar);

            // Homogeneous point in 3D world space
            Vector4 aPt = new Vector4(-2f, 2f, -10f, 1f);

            aPt *= viewMatrix; // To view coordinate space (VCS)

            aPt *= projectionMatrix; // To clipping coordinate space (CCS), NPC (normalized projection coordinates)

            // To get the 2D coordinates on the screen
            Single screenW = 100f;
            Single screenH = 100f;
            Single xScreen = (aPt.X / aPt.W) * screenW; // Perspective division
            Single yScreen = (aPt.Y / aPt.W) * screenH;
        }
#endif
    }
}
