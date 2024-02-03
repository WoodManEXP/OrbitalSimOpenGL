using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    internal class PathPoints
    {
        /// <summary>
        /// Points along the path are held in circular buffer
        /// </summary>
        /// <remarks>
        /// Circular list. If list is full newest replaces oldest
        /// </remarks>
        public Int16 NumPoints { get; private set; } = 0;
        private Int16 NextPosn { get; set; } = 0;
        public Int16 MaxNumPoints { get; set; }

        public Vector3d[] Points; // The path points

        public PathPoints(Int16 numPoints = 500)
        {
            MaxNumPoints = numPoints;
            Points = new Vector3d[numPoints];
        }

        /// <summary>
        /// Add another trace point
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <remarks>
        /// </remarks>
        public void AddOne(Double X, Double Y, Double Z)
        {
            Points[NextPosn].X = X; Points[NextPosn].Y = Y; Points[NextPosn].Z = Z;

            if (NumPoints < MaxNumPoints)
                NumPoints++;

            NextPosn++;
            NextPosn %= MaxNumPoints; // Wrap
        }
    }

    /// <summary>
    /// Provides visual path tracing of a body in the 3D space
    /// </summary>
    /// <remarks>
    /// This becomes quite consumptative in terms of memory and processing
    /// - Retaining/managing path history
    /// - Watching for changes in path angle
    /// - Culling to visible and non-visible path points
    /// - Pushng points to OpenGL for rendering
    /// </remarks>
    internal class PathTracer
    {
        #region Properties
        static Double OneAU { get; } = 1.49668992E8D; // KM (93M miles);
        private static Double DistIncrement { get; } = OneAU / 6D;
        private static Single TracePointSize { get; } = 1.5F;

        private int Vector3Size = Marshal.SizeOf(typeof(Vector3));

        // Through what angle should the path traverse in order to place another path trace visual
        private static Double CosThreshold { get; } = Math.Cos(MathHelper.DegreesToRadians(5D)); // 5 degrees

        private Vector3d LastPos;       // Last tracept position
        private Vector3d LastPosVelVec; // Velocity vec at last travept position
        private Vector3d LastVelVec;    // Last velocity vec passed to :AddLoc
        private Double DistSoFar { get; set; } = 0D;
        private bool FirstTime = true;
        private PathPoints PathPoints { get; set; }
        private Scale Scale { get; set; } // For universe to WPF coords
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scale"></param>
        public PathTracer(Scale scale)
        {
            Scale = scale;
            PathPoints = new();
        }

        /// <summary>
        /// Add a location, potentially, to the path
        /// </summary>
        /// <param name="seconds">Num of seconds in the interval that brings the body to this point</param>
        /// <param name="x">UCoords</param>
        /// <param name="y">UCoords</param>
        /// <param name="z">UCoords</param>
        /// <param name="vX">Velocity component km/s</param>
        /// <param name="vY">Velocity component km/s</param>
        /// <param name="vZ">Velocity component km/s</param>
        /// <remarks>
        /// Loss of precision is too high using Vector3d.NormalizeFast()
        /// </remarks>
        public void AddLoc(int seconds, Double x, Double y, Double z, Double vX, Double vY, Double vZ)
        {
            if (FirstTime)
            {
                // First time
                FirstTime = false;
                LastPos.X = x; LastPos.Y = y; LastPos.Z = z;
                LastVelVec.X = vX; LastVelVec.Y = vY; LastVelVec.Z = vZ;
                LastPosVelVec = LastVelVec;
                LastPosVelVec.Normalize();
                return;
            }

            // Distance traveled at the previous velocity
            DistSoFar += seconds * LastVelVec.LengthFast;
            LastVelVec.X = vX; LastVelVec.Y = vY; LastVelVec.Z = vZ;

            // If new loc is far enough from last trace point or if the angle through which velocity vector has moved
            // since last trace point croses the cos threshold, the loc will be recorded in the path trace

            // Expensive calculation as it is called on each reposition
            if (DistIncrement <= DistSoFar || ArcThresholdCrossed(vX, vY, vZ))
            {
                // Add to PathPoints
                PathPoints.AddOne(x, y, z);

                // Prep for next
                LastPos.X = x; LastPos.Y = y; LastPos.Z = z;
                LastPosVelVec.X = vX; LastPosVelVec.Y = vY; LastPosVelVec.Z = vZ;
                LastPosVelVec.Normalize();
                DistSoFar = 0D;
            }
        }

        /// <summary>
        /// Does the velVec sent in cross the cos threshold relative to the velVec recorded with the
        /// previous trace point?
        /// </summary>
        /// <param name="vX"></param>
        /// <param name="vY"></param>
        /// <param name="vZ"></param>
        /// <returns>True or False</returns>
        /// <remarks>
        /// Expensive calculation
        /// </remarks>
        private bool ArcThresholdCrossed(Double vX, Double vY, Double vZ)
        {
            // Through what angle has the velocity vector traversed since last pathpoint?
            Vector3d cVec = new(vX, vY, vZ);
            cVec.Normalize(); // NormalizeFast not accurate enough...

            // Dot product yields cos. Same calculation as Vector3d.Dot()
            Double cos = Math.Abs(LastPosVelVec.X * cVec.X + LastPosVelVec.Y * cVec.Y + LastPosVelVec.Z * cVec.Z);

            return (CosThreshold >= cos);  // cos <= CosThreshold
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// https://docs.gl/gl4/glDrawElements
        /// https://stackoverflow.com/questions/44687061/how-to-draw-points-efficiently
        /// https://stackoverflow.com/questions/11821336/what-are-vertex-array-objects
        /// Use the Frustum culler to remove/include tracepoints visible in the current frustum prior to OpenGL load.
        /// A point is a sphere of 0 diameter, so FrustumCiller:SphereCulls can be used.
        /// </remarks>
        public void Render(FrustumCuller fC, Color4 bodyColor, int bodyColorUniform, int mvp_Uniform, ref Matrix4 vp)
        {

            if (0 < PathPoints.NumPoints)
            {
                // Any/all visible path points will be copied to this Single precision vertex array
                Vector3[] worldPoints = new Vector3[PathPoints.MaxNumPoints]; // Stack space
                Int16 numWorldPoints = 0;

                // Pull in points to worldPoints array that are visible in current frustum
                // Also scle from UCoords to WCoords (OpenGL coords)
                for (Int16 i = 0; i < PathPoints.NumPoints; i++)
                    if (!fC.SphereCulls(ref PathPoints.Points[i], 0D))
                        Scale.ScaleU_ToW(out worldPoints[numWorldPoints++], PathPoints.Points[i].X, PathPoints.Points[i].Y, PathPoints.Points[i].Z);

                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3Size, 0 /*GL.GetAttribLocation(Shader.ShaderHandle, "aPosition")*/);
                GL.BufferData(BufferTarget.ArrayBuffer, numWorldPoints * Vector3Size, worldPoints, BufferUsageHint.StaticDraw);

                GL.Uniform4(bodyColorUniform, bodyColor);
                GL.UniformMatrix4(mvp_Uniform, false, ref vp);

                Single ptSize = GL.GetFloat(GetPName.PointSize);
                GL.PointSize(TracePointSize);
                GL.DrawArrays(PrimitiveType.Points, 0, numWorldPoints);
                GL.PointSize(ptSize);
            }
        }
    }
}
