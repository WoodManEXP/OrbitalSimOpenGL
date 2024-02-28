using OpenTK.Mathematics;
using System;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Large coordinate systems really mess with various projection calculations.
    /// All calculations are done at system scale. But prior to being sent to OpenGL
    /// the various visual coordinates are scaled down to a range that seems to suit better.
    /// </summary>
    public class Scale
    {
        #region Properties
        public Double ScaleVal { get; set; } // For scaling from universe to WPF coords

        private int _camMoveAmt; // For scaling camera movements
        public int CamMoveAmt
        {
            get { return _camMoveAmt; }
            set
            {
                _camMoveAmt = value;
                if (value >= Properties.Settings.Default.MaxCamMoveScale || value < 0)
                    throw new InvalidOperationException("Scale: Camera out of scale");
            }
        }
        #endregion

        public Scale()
        {
            ScaleVal = 1E-05; // 1
            CamMoveAmt = 14; // ~1M km Exp(14)
        }

        /// <summary>
        /// Scale Double from Universe to OpenGL coords
        /// </summary>
        /// <param name="d">in Unverse coords</param>
        /// <returns>Result scaled to Single in OpenGL coords</returns>
        public Single ScaleU_ToW(Double d)
        {
            return (Single)(d * ScaleVal);
        }

        /// <summary>
        /// Scale an existing Point3D
        /// </summary>
        /// <param name="point3D"></param>
        /// <returns></returns>
        public Point3D ScaleU_ToW(Point3D point3D)
        {
            point3D.X *= ScaleVal;
            point3D.Y *= ScaleVal;
            point3D.Z *= ScaleVal;
            return point3D;
        }

        /// <summary>
        /// Create a scaled Point3D
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public void ScaleU_ToW(out Vector3 sPoint3D, Double x, Double y, Double z)
        {
            sPoint3D.X = (float)(x * ScaleVal);
            sPoint3D.Y = (float)(y * ScaleVal);
            sPoint3D.Z = (float)(z * ScaleVal);
        }

        /// <summary>
        /// Scale array of UCords to WCoords
        /// </summary>
        /// <param name="sPoints3D"></param>
        public void ScaleU_ToW(ref Vector3[] sPoints, ref Vector3d[] iPoints)
        {
            for (int i = 0; i < sPoints.Length; i++)
            {
                sPoints[i].X = (float)(iPoints[i].X * ScaleVal);
                sPoints[i].Y = (float)(iPoints[i].Y * ScaleVal);
                sPoints[i].Z = (float)(iPoints[i].Z * ScaleVal);
            }
        }

        public void ScaleU_ToW(ref Vector3 sPoint3D, Vector3d vec)
        {
            sPoint3D.X = (float)(vec.X * ScaleVal);
            sPoint3D.Y = (float)(vec.Y * ScaleVal);
            sPoint3D.Z = (float)(vec.Z * ScaleVal);
        }

        public void ScaleU_ToW(ref Vector3d sPoint3D, Vector3d vec)
        {
            sPoint3D.X = vec.X * ScaleVal;
            sPoint3D.Y = vec.Y * ScaleVal;
            sPoint3D.Z = vec.Z * ScaleVal;
        }

        /// <summary>
        /// Scale an existing Vector3D
        /// </summary>
        /// <param name="vector3D"></param>
        /// <returns></returns>
        public Vector3d ScaleVector3D(ref Vector3d vector3D)
        {
            vector3D.X *= ScaleVal;
            vector3D.Y *= ScaleVal;
            vector3D.Z *= ScaleVal;
            return vector3D;
        }

        /// <summary>
        /// Create a scaled Vector3D
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public Vector3d ScaleVector3D(Double x, Double y, Double z)
        {
            Vector3d v = new(x, y, z);
            return ScaleVector3D(ref v);
        }
    }
}
