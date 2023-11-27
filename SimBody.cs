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

        // Next settings, U Coords
        public Double N_X { get; set; }
        public Double N_Y { get; set; }
        public Double N_Z { get; set; }
        public Double N_VX { get; set; }
        public Double N_VY { get; set; }
        public Double N_VZ { get; set; }

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
            VY = double.TryParse(ephemerisBody.VY_Str, out dVal) ? dVal : -1D;
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
                //"Phobos" => Color4.Green,
                //"Deimos" => Color4.Green,
                "Mars" => Color4.DarkRed,
                "Jupiter" => Color4.Beige,
                "Saturn" => Color4.Beige,
                "Uranus" => Color4.LightGreen,
                "Neptune" => Color4.SlateBlue,
                "Pluto" => Color4.Tan,
                _ => Color4.Green
            };
        }

        /// <summary>
        /// Render body using the shared sphere
        /// </summary>
        /// <param name="indicesLength"></param>
        /// <param name="bodyColorUniform"></param>
        /// <param name="mvp_Uniform"></param>
        /// <param name="vp"></param>
        /// <param name="locationMatrix4"></param>
        /// <param name="sizeMatrix4"></param>
        internal void RenderSphericalBody(int indicesLength, int bodyColorUniform, int mvp_Uniform, ref Matrix4 vp, ref Matrix4 locationMatrix4, ref Matrix4 sizeMatrix4)
        {
            locationMatrix4.M41 = Scale.ScaleU_ToW(X); // X
            locationMatrix4.M42 = Scale.ScaleU_ToW(Y); // Y
            locationMatrix4.M43 = Scale.ScaleU_ToW(Z); // Z

            sizeMatrix4.M11 =  sizeMatrix4.M22 =  sizeMatrix4.M33 = Scale.ScaleU_ToW(EphemerisDiameter);

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
        /// <param name="m">World to viewport transform matrix</param>
        /// <param name="halfNorm">Half camera's UpDirection vector</param>
        /// <param name="minSize">Minimum desired size</param>
        /// <param name="minSizeSquared">Square of the minimum desired size</param>
        public void KeepVisible(Matrix3D m, Vector3D halfNorm, Double minSize, Double minSizeSquared)
        {
            // Calc Point3D coordinates for both ends of a vector parallel to camera's UpDirection,
            // centered at sB.X, sB.Y, sB.Z with length = sB's diameter. These represents widest
            // points of sphere as seen by the camera.
            Point3D oneSideUniv = new(X + HalfEphemerisD * halfNorm.X, Y + HalfEphemerisD * halfNorm.Y, Z + HalfEphemerisD * halfNorm.Z);
            Point3D otherSideUniv = new(X - HalfEphemerisD * halfNorm.X, Y - HalfEphemerisD * halfNorm.Y, Z - HalfEphemerisD * halfNorm.Z);

            Point3D oneSideView = m.Transform(Scale.ScaleU_ToW(oneSideUniv));        // Universe to WPF coords
            Point3D otherSideView = m.Transform(Scale.ScaleU_ToW(otherSideUniv));

            Double dX = oneSideView.X - otherSideView.X;
            Double dY = oneSideView.Y - otherSideView.Y;
            Double distSquared = dX * dX + dY * dY;

            if (distSquared < minSizeSquared)
            {
                // Change the body's diameter such that distSquared will transform to minSizeSquared
                UseD = minSize * EphemerisDiameter / Math.Sqrt(distSquared);
            }
            else
            {
                // Not too far away, ensure the diameter is the original value
                UseD = EphemerisDiameter;
            }

            // Update the DiameterTransform3D for UseD value
            //DiameterTransform3D.ScaleX = DiameterTransform3D.ScaleY = DiameterTransform3D.ScaleZ = Scale.ScaleU_ToW(UseD);
        }
    }
}
