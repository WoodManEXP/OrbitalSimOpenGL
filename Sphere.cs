using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Media3D;

/*
 * http://csharphelper.com/howtos/howto_3D_sphere.html
 * http://csharphelper.com/howtos/howto_3D_smooth_sphere.html
 * 
 */
namespace OrbitalSimOpenGL
{
    public class Sphere
    {
        // Add a sphere.
        public static void AddSphere(Point3DCollection mesh, Int32Collection indices,  Point3D center,
            double radius, int num_phi, int num_theta)
        {
            double phi0, theta0;
            double dphi = Math.PI / num_phi;
            double dtheta = 2 * Math.PI / num_theta;

            Dictionary<Point3D, int> dictionary = new();

            phi0 = 0;
            double y0 = radius * Math.Cos(phi0);
            double r0 = radius * Math.Sin(phi0);
            for (int i = 0; i < num_phi; i++)
            {
                double phi1 = phi0 + dphi;
                double y1 = radius * Math.Cos(phi1);
                double r1 = radius * Math.Sin(phi1);

                // Point ptAB has phi value A and theta value B.
                // For example, pt01 has phi = phi0 and theta = theta1.
                // Find the points with theta = theta0.
                theta0 = 0;
                Point3D pt00 = new Point3D(
                    center.X + r0 * Math.Cos(theta0),
                    center.Y + y0,
                    center.Z + r0 * Math.Sin(theta0));
                Point3D pt10 = new Point3D(
                    center.X + r1 * Math.Cos(theta0),
                    center.Y + y1,
                    center.Z + r1 * Math.Sin(theta0));
                for (int j = 0; j < num_theta; j++)
                {
                    // Find the points with theta = theta1.
                    double theta1 = theta0 + dtheta;
                    Point3D pt01 = new Point3D(
                        center.X + r0 * Math.Cos(theta1),
                        center.Y + y0,
                        center.Z + r0 * Math.Sin(theta1));
                    Point3D pt11 = new Point3D(
                        center.X + r1 * Math.Cos(theta1),
                        center.Y + y1,
                        center.Z + r1 * Math.Sin(theta1));

                    // Create the triangles.
                    AddSmoothTriangle(mesh, indices, dictionary, pt00, pt11, pt10);
                    AddSmoothTriangle(mesh, indices, dictionary, pt00, pt01, pt11);

                    // Texture coords are useful if/when an ImageBrush is used
                    //mesh.TextureCoordinates.Add(new System.Windows.Point((double)j / num_theta, (double)i / num_phi)); // texture coordinates are scaled so they range from 0 to 1 for the phi and theta values

                    // Move to the next value of theta.
                    theta0 = theta1;
                    pt00 = pt01;
                    pt10 = pt11;
                }

                // Move to the next value of phi.
                phi0 = phi1;
                y0 = y1;
                r0 = r1;
            }
        }

        // Add a triangle to the indicated mesh.
        // Reuse points so triangles share normals.
        private static void AddSmoothTriangle(Point3DCollection mesh, Int32Collection indices,
            Dictionary<Point3D, int> dict, Point3D point1, Point3D point2, Point3D point3)
        {
            int index1, index2, index3;

            // Find or create the points.
            if (dict.ContainsKey(point1)) index1 = dict[point1];
            else
            {
                index1 = mesh.Count;
                mesh.Add(point1);
                dict.Add(point1, index1);
            }

            if (dict.ContainsKey(point2)) index2 = dict[point2];
            else
            {
                index2 = mesh.Count;
                mesh.Add(point2);
                dict.Add(point2, index2);
            }

            if (dict.ContainsKey(point3)) index3 = dict[point3];
            else
            {
                index3 = mesh.Count;
                mesh.Add(point3);
                dict.Add(point3, index3);
            }

            // If two or more of the points are
            // the same, it's not a triangle.
            if ((index1 == index2) ||
                (index2 == index3) ||
                (index3 == index1)) return;

            // Create the triangle.
            indices.Add(index1);
            indices.Add(index2);
            indices.Add(index3);
        }
    }
}
