using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Each SimBody reoresents a body in the simulation
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
        public Double UseDiameter { get; set; } // U coords, gets adjusted for visability
        public Double HalfEphemerisDiameter { get; set; }
        public Double Mass { get; set; }
        public Double GM { get; set; }
        public string ID { get; private set; }
        public string Name { get; private set; }

        // For trace path drawing
        public Vector3D LastTraceVector3D;      // Watching curvature rate for drawing path "lines"
        public Point3D LastTracePoint3D;        // U coords

        Color4 BodyColor { get; set; }
        private String AppDataFolder { get; set; }

        public bool ExcludeFromSim { get; set; } = false; // Body is to be or not be excluded from the sim
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
            HalfEphemerisDiameter = EphemerisDiameter / 2D;
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

            sizeMatrix4.M11 = sizeMatrix4.M22 = sizeMatrix4.M33 = Scale.ScaleU_ToW(UseDiameter);

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

        //        int iCtr = 0;

        /// <summary>
        /// If body has become too small in 3D to 2D projection adjust its size so it remains visible.
        /// And hit testing.
        /// </summary>
        /// <param name="simCamera">Camera</param>
        /// <param name="halfNorm">Camera's UpVector * .5 </param>
        /// <param name="minSize">min pixel diameter for rendered bodies</param>
        /// <param name="minSizeSquared">min pixel diameter squared for rendered bodies</param>
        /// <param name="mousePosition">Current mouse cursor position, for hit-testing</param>
        /// <returns>
        /// -1D if 2D mouseCursor position is NOT over the rendered body.
        /// Distance, in km, from cameraPosition to point on sphere 2D mouseCursor position is over.
        /// Can be used as a Z value to determine which hit to use (if there are multiples).
        /// renderingPrecision - some calculations are lost in single precision (e.g. small bodies + large distances). If
        /// that condition is detected renderingPrecision will be set to DoublePrecision, otherwise SinglePrecision.
        /// </returns>
        /// <remarks>
        /// This works only on spheres. The hit testing is a poor-man's RayCasting. Becasue each body visible in the 
        /// frustum is examined before being sent to OpenGL an inexpensive oppty avails to perfom hit testing.
        /// All calculations via double precision.
        /// </remarks>
        public Double KeepVisible(SimCamera simCamera, ref Vector3d halfNorm, Single minSize, Single minSizeSquared,
            ref System.Windows.Point mousePosition)
        {
            // Center of body in Normalized Device Coords
            Single distSquared;

            // Small bodies are lost in Single precision rounding errors.
            // In these cases jump the calculation to Double precision.

            Double distSquaredD;
            DistSquaredD(simCamera, HalfEphemerisDiameter, ref halfNorm, minSize, out distSquaredD, out Vector2 ndcCenter);
            distSquared = (Single)distSquaredD;

            //                Single distSquared;
            //                DistSquared(simCamera, HalfEphemerisDiameter, ref halfNorm, minSize, out distSquared, out ndcCenter);

            if (distSquaredD < (Double)minSizeSquared)
                // Change the body's diameter such that distSquared will transform to minSizeSquared
                UseDiameter = (minSize * EphemerisDiameter) / Math.Sqrt(distSquaredD);
            else
                // Not too small, keep the diameter as the original value
                UseDiameter = EphemerisDiameter;

            // What is new distSquareD after diameter adjustment
            //Double newDistSquaredD;
            //DistSquaredD(simCamera, UseDiameter / 2D, ref halfNorm, minSize, out newDistSquaredD, out ndcCenter);

            //Single newDistSquared;
            //DistSquared(simCamera, UseDiameter / 2D, ref halfNorm, minSize, out newDistSquared, out ndcCenter);

            // Hit-test section
            // OpenGL's window-space is relative to the bottom-left of the window, not the top-left as in Windows.
            Single cX = ((ndcCenter.X + 1F) / 2F) * simCamera.ViewWidth;
            Single cY = ((1F - ndcCenter.Y) / 2F) * simCamera.ViewHeight;

            Single distSquared2 = (distSquared < minSizeSquared) ? minSizeSquared : distSquared;

            // distSquared from mouse cursor position to Center of sphere as projected onto 2D
            Single mX = (Single)mousePosition.X - cX; mX *= mX;
            Single mY = (Single)mousePosition.Y - cY; mY *= mY;
            Single mouseDistSquared = mX + mY;

            //GL.Viewport  GL_VIEWPORT

            //            int[] iArray = new int[4];
            //            GL.GetInteger(GetPName.Viewport, iArray);
#if false
            if (0 == ++Ctr % 100)
                System.Diagnostics.Debug.WriteLine("SimBody.KeepVisible - " + Name
                    + " distSquaredInt " + distSquared.ToString()
                    + " mouseDistSquared " + mouseDistSquared.ToString()
                    + " cX, cY " + cX.ToString() + ", " + cY.ToString()
                    + " mouseX, mouseY " + mousePosition.X.ToString() + ", " + mousePosition.Y.ToString()
                    + ((mouseDistSquared <= distSquared) ? " Hit" : " No hit")
                    );
#endif
            Double dist;
            if (mouseDistSquared <= distSquared2)
            {
                // A hit
                Vector3D distVector3D;
                distVector3D.X = this.X - simCamera.CameraPosition.X;
                distVector3D.Y = this.Y - simCamera.CameraPosition.Y;
                distVector3D.Z = this.Z - simCamera.CameraPosition.Z;

                // Dist from camera position to point on sphere where ray cast would intersect
                dist = distVector3D.Length - (EphemerisDiameter / 2);
            }
            else
                dist = -1;

            return dist;
        }

        /// <summary>
        /// Calculate distSquared in double precision
        /// </summary>
        /// <param name="simCamera"></param>
        /// <param name="diameter"></param>
        /// <param name="halfNorm"></param>
        /// <param name="minSize"></param>
        /// <param name="distSquared"></param>
        /// <param name="pixelDiameter"></param>
        /// <param name="ndcCenter"></param>
        private void DistSquaredD(SimCamera simCamera, Double halfD, ref Vector3d halfNorm, Single minSize, out Double distSquared,
                    out Vector2 ndcCenter)
        {
            // Calc Point3D coordinates for both ends of a vector parallel to camera's UpDirection,
            // centered at sB.X, sB.Y, sB.Z with length = sB's diameter. These represents widest
            // points of sphere as seen by the camera.
            Vector3d oneSideUniv = new(X + halfD * halfNorm.X, Y + halfD * halfNorm.Y, Z + halfD * halfNorm.Z);
            Vector3d otherSideUniv = new(X - halfD * halfNorm.X, Y - halfD * halfNorm.Y, Z - halfD * halfNorm.Z);

            // U coords to W coords
            Scale.ScaleVector3D(ref oneSideUniv);
            Scale.ScaleVector3D(ref otherSideUniv);

            // Make 3D to Homogenous 4D
            Vector4d oneSideV4 = new(oneSideUniv, 1d);
            Vector4d otherSideV4 = new(otherSideUniv, 1d);

            // To clip space (normalized device coordinates), -1 .. 1 (through View and projection matrices)
            Vector4d oneSide = oneSideV4 * simCamera.VP_MatrixD;
            Vector4d otherSide = otherSideV4 * simCamera.VP_MatrixD;
            oneSide /= oneSide.W;
            otherSide /= otherSide.W;

            // Retain center point ndc, normalized device coordinates, (for hit-testing below)
            ndcCenter = new((Single)((oneSide.X + otherSide.X) / 2D), (Single)((oneSide.Y + otherSide.Y) / 2D));

            // To pixel values
            // https://stackoverflow.com/questions/8491247/c-opengl-convert-world-coords-to-screen2d-coords
            Double oneSideX = ((oneSide.X + 1D) / 2D) * simCamera.ViewWidth;
            Double oneSideY = ((oneSide.Y + 1D) / 2D) * simCamera.ViewHeight;
            Double otherSideX = ((otherSide.X + 1D) / 2D) * simCamera.ViewWidth;
            Double otherSideY = ((otherSide.Y + 1D) / 2D) * simCamera.ViewHeight;

            Double dX = oneSideX - otherSideX;
            Double dY = oneSideY - otherSideY;
            distSquared = dX * dX + dY * dY;
        }

#if false
        /// <summary>
        /// Calculate distSquared in double precision
        /// </summary>
        /// <param name="simCamera"></param>
        /// <param name="diameter"></param>
        /// <param name="halfNorm"></param>
        /// <param name="minSize"></param>
        /// <param name="distSquared"></param>
        /// <param name="pixelDiameter"></param>
        /// <param name="ndcCenter"></param>
        private void DistSquared(SimCamera simCamera, Double halfD, ref Vector3d halfNorm, Single minSize, out Single distSquared,
            out Vector2 ndcCenter)
        {
            // Calc Point3D coordinates for both ends of a vector parallel to camera's UpDirection,
            // centered at sB.X, sB.Y, sB.Z with length = sB's diameter. These represents widest
            // points of sphere as seen by the camera.
            Vector3d oneSideUniv = new(X + halfD * halfNorm.X, Y + halfD * halfNorm.Y, Z + halfD * halfNorm.Z);
            Vector3d otherSideUniv = new(X - halfD * halfNorm.X, Y - halfD * halfNorm.Y, Z - halfD * halfNorm.Z);

            // U coords to W coords
            Scale.ScaleVector3D(ref oneSideUniv);
            Scale.ScaleVector3D(ref otherSideUniv);

            // 3D to homogeneous 4D
            Vector4 oneSideV4 = new((Vector3)oneSideUniv, 1f);
            Vector4 otherSideV4 = new((Vector3)otherSideUniv, 1f);

            // To clip space (normalized device coordinates), -1 .. 1 (through View and projection matrices)
            Vector4 oneSide = oneSideV4 * simCamera.VP_Matrix;
            Vector4 otherSide = otherSideV4 * simCamera.VP_Matrix;
            oneSide /= oneSide.W;
            otherSide /= otherSide.W;

            // Retain center point ndc, normalized device coordinates, (for hit-testing below)
            ndcCenter = new((oneSide.X + otherSide.X) / 2F, (oneSide.Y + otherSide.Y) / 2F);

            // To pixel values
            // https://stackoverflow.com/questions/8491247/c-opengl-convert-world-coords-to-screen2d-coords
            Single oneSideX = ((oneSide.X + 1F) / 2F) * simCamera.ViewWidth;
            Single oneSideY = ((oneSide.Y + 1F) / 2F) * simCamera.ViewHeight;
            Single otherSideX = ((otherSide.X + 1F) / 2F) * simCamera.ViewWidth;
            Single otherSideY = ((otherSide.Y + 1F) / 2F) * simCamera.ViewHeight;

            Single dX = oneSideX - otherSideX;
            Single dY = oneSideY - otherSideY;
            distSquared = dX * dX + dY * dY;
        }
#endif
    }
}
