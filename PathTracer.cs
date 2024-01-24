using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    class PathPoints
    {
        /// <summary>
        /// Points along the path are held in chunks of these
        /// </summary>
        private int _NumPoints;
        public int NumPoints { get { return _NumPoints; } }
        public int MaxNumPoints { get; }
        public Vector3[] Point; // UCoords converted to OpenGL range

        private readonly Scale Scale;

        public PathPoints(Scale scale, int chunkSize)
        {
            _NumPoints = 0;
            MaxNumPoints = chunkSize;
            Point = new Vector3[chunkSize];    // This illustrates a short coming of C#. Seems like withn a class/struct it
                                               // should be possible to place the number of array elements directly in the
                                               // definition rather than requiring a separate heap allocation.
            Scale = scale;
        }

        /// <summary>
        /// Add another trace point
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <remarks>
        /// Trace points are stored as Single (preconverted to OpenGL coords), saving
        /// on both space and calcuations.
        /// </remarks>
        public void AddOne(Double X, Double Y, Double Z)
        {
            if (_NumPoints < MaxNumPoints) // JIC
                Scale.ScaleU_ToW(out Point[_NumPoints++], X, Y, Z);
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

        // Through what angle should the path traverse in order to place another path trace visual
        private /*static*/ Double CosThreshold { get; } = Math.Cos(MathHelper.DegreesToRadians(5D)); // 5 degrees

        private Vector3d LastPos;       // Last tracept position
        private Vector3d LastPosVelVec; // Velocity vec at last travept position
        private Vector3d LastVelVec;    // Last velocity vec passed to :AddLoc
        private Double DistSoFar { get; set; } = 0D;
        private bool FirstTime = true;
        private Scale Scale { get; set; } // For universe to WPF coords

        // Path points chunks held in this List
        static readonly int PathPointsChunkSize = 200;
        public List<PathPoints> PointsList { get; } = new(PathPointsChunkSize);
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathTraceModelVisual3D">The SimModelVisual3D into which to place the path elements</param>
        public PathTracer(Scale scale)
        {
            Scale = scale;

            // Add first points chunk to the list
            PointsList.Add(new(Scale, PathPointsChunkSize));
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
                int count = PointsList.Count;
                PathPoints p = PointsList[count - 1];
                if (p.NumPoints >= p.MaxNumPoints)
                {
                    // Start a new chunk
                    PointsList.Add(p = new(Scale, PathPointsChunkSize));
                }

                p.AddOne(x, y, z);

                // Prep for next
                LastPos.X = x; LastPos.Y = y; LastPos.Z = z;
                LastPosVelVec.X = vX; LastPosVelVec.Y = vY; LastPosVelVec.Z = vZ;
                LastPosVelVec.Normalize();
                DistSoFar = 0D;

//                System.Diagnostics.Debug.WriteLine("PathTracer:AddLoc: "
//                            + " Crossed threshold " + crossedThreshold.ToString()
//                            );
            }
        }

//        long iCtr = -1L;

        /// <summary>
        /// Does the velVec sent in cross the cos threshold relative to the velVec recorded with the
        /// previous trace point?
        /// </summary>
        /// <param name="vX"></param>
        /// <param name="vY"></param>
        /// <param name="vZ"></param>
        /// <returns></returns>
        /// <remarks>
        /// Expensive calculation
        /// </remarks>
        private bool ArcThresholdCrossed(Double vX, Double vY, Double vZ)
        {
            // Through what angle has the velocity vector traversed since last pathpoint?
            Vector3d cVec = new(vX, vY, vZ);
            cVec.Normalize();
            Double cos = Math.Abs(LastPosVelVec.X * cVec.X + LastPosVelVec.Y * cVec.Y + LastPosVelVec.Z * cVec.Z);

//            Double lRadians = Math.Acos(cos);
//            Double lDegrees = MathHelper.RadiansToDegrees(lRadians);

//            if (0 == ++iCtr % 200)
//                System.Diagnostics.Debug.WriteLine("PathTracer:CosThresholdCrossed: "
//                    + iCtr.ToString()
//                    + " DistIncrement " + DistIncrement.ToString("N0")
//                    + " DistSoFar " + DistSoFar.ToString("N0")
//                    + " my cos " + cos.ToString()
//                    + " my radians " + lRadians.ToString()
//                    + " my degrees " + lDegrees.ToString()
//                );

            return (CosThreshold >= cos);  // cos <= CosThreshold
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// https://docs.gl/gl4/glDrawElements
        /// </remarks>
        public void Render()
        {

        }
    }
}
