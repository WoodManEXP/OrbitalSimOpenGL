using System;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using OpenTK.Mathematics;
using System.Windows.Controls;
using System.Windows;
using System.Linq;
using OpenTK.Graphics.OpenGL;

namespace OrbitalSimOpenGL
{
    internal class Util
    {

        public static Double G_KM { get; } = 1E-3 * 6.6743E-11;     // Gravitational constant is typically offered in kg*m/s-squared
                                                                    // The 1E-3 converts from kg*m/s-squared to kg*km/s-squared
                                                                    // (sim distancs are in km rather than m)
        public static Double G_M { get; } = 6.6743E-11;            // Typically seen value kg m / sec-squared
        public static Double C_KMS { get; } = 299792D;  // Speed of light 299,792 km/s
        public static Double C_M { get; } = 299792458D;  // Speed of light 299,792,458 m/s
        public static Double CSquared_M { get; } = C_M * C_M;
        public static Double C_OneTenthKMS { get; } = C_KMS * 1D - 1; // 1/10th C km/s
        public static Double C_OneTentSquaredhKMS { get; } = C_OneTenthKMS * C_OneTenthKMS;

        static public void MakeUnitSphere(out float[] sharedSphereMesh, out UInt16[] sharedSphereIndices)
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

        /// <summary>
        /// Search UIElement tree for element with specific Uid
        /// </summary>
        /// <param name="rootElement"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://stackoverflow.com/questions/1887611/get-object-by-its-uid-in-wpf
        /// </remarks>
        public static UIElement GetByUid(DependencyObject rootElement, string uid)
        {
            foreach (UIElement element in LogicalTreeHelper.GetChildren(rootElement).OfType<UIElement>())
            {
                if (element.Uid == uid)
                    return element;
                UIElement resultChildren = GetByUid(element, uid);
                if (resultChildren != null)
                    return resultChildren;
            }
            return null;
        }
    }
}
