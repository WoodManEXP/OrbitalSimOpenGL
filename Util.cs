using System;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using OpenTK.Mathematics;
using System.Windows.Controls;

namespace OrbitalSimOpenGL
{
    internal class Util
    {
        static public void MakeUnitSphere(ref float[]? sharedSphereMesh, ref UInt16[]? sharedSphereIndices)
        {
            Point3DCollection mesh = new();
            Int32Collection indices = new();
            Sphere.AddSphere(mesh, indices, new(0D, 0D, 0D), .5, 10, 10);

            // Cvt to mesh/vertex and indices into form needed by OpenGL
            sharedSphereMesh = new Single[3 * mesh.Count];
            for (int i = 0, m = 0; i < mesh.Count; i++, m += 3)
            {
                sharedSphereMesh[m + 0] = (Single)mesh[i].X;
                sharedSphereMesh[m + 1] = (Single)mesh[i].Y;
                sharedSphereMesh[m + 2] = (Single)mesh[i].Z;
            }

            sharedSphereIndices = new UInt16[indices.Count];
            for (int i = 0; i < indices.Count; i++)
                sharedSphereIndices[i] = (UInt16)indices[i];
        }

        /// <summary>
        /// Make an OpenGL/OpenTK Quaternion but using WPF 3D's input parameter pattern
        /// </summary>
        /// <param name="axisOfRotation"></param>
        /// <param name="angleInRadians"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        static public OpenTK.Mathematics.Quaterniond MakeQuaterniond(Vector3d axisOfRotation, Double angleInRadians)
        {
            Double length = axisOfRotation.Length;
            if (length == 0)
                throw new System.InvalidOperationException("Zero length rotation vector");
            Vector3d v = (axisOfRotation / length) * (float)Math.Sin(0.5 * angleInRadians);
            Double w = Math.Cos(0.5 * angleInRadians);

            return new OpenTK.Mathematics.Quaterniond(v, (float)w);
        }

        /// <summary>
        /// Make an OpenGL/OpenTK Quaternion but using WPD 3D's input parameter pattern
        /// </summary>
        /// <param name="axisOfRotation"></param>
        /// <param name="angleInRadians"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        static public OpenTK.Mathematics.Quaternion MakeQuaternion(Vector3 axisOfRotation, float angleInRadians)
        {
            float length = axisOfRotation.Length;
            if (length == 0)
                throw new System.InvalidOperationException("Zero length rotation vector");
            Vector3 v = (axisOfRotation / length) * (float)Math.Sin(0.5 * angleInRadians);
            float w = (float)Math.Cos(0.5 * angleInRadians);

            return new OpenTK.Mathematics.Quaternion(v, w);
        }
    }
}
