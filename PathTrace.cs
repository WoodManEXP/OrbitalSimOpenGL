using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Provides visual path tracing of the bodies in the 3D space
    /// </summary>
    internal class PathTrace
    {
        #region Properties
        private Boolean _TracePaths;
        private ulong TraceSegments = 0;
        private readonly Double CurvatureCos = Math.Cos(10D * (Math.PI / 180)); // 10 degrees threshold cos(10) = 0.984807753012...
        public Boolean TracePaths
        {
            get { return _TracePaths; }
            set { SetTracePaths(value); }
        }

        // Path element mesh and color (both reused over and over)

        private Scale Scale; // For universe to WPF coords
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathTraceModelVisual3D">The SimModelVisual3D into which to place the path elements</param>
        public PathTrace(Scale scale)
        {
            TracePaths = false;
            Scale = scale;

            InitPathElement();
        }

        // Set path tracing on or off
        private void SetTracePaths(Boolean value)
        {
            _TracePaths = value;

            if (!TracePaths)
            {
                // Tracing is off, remove all the path elements from the PathTraceModelVisual3D
            }
        }

        /// <summary>
        /// See if the path has curved enugh to warrant adding a path trace segment.
        /// </summary>
        /// <remarks>
        /// With objects being moved incrementally it is possible to monitor the angle
        /// between individual movement vectors as curvature check. A past vector is retained.
        /// When a new vector is over a threshold angle from the past vector the curvature
        /// warrants adding a path trace segment to the model. The newest vector is retained and the process
        /// repeats.
        /// </remarks>
        /// <param name="simBodyList"></param>
        /// <param name="iterationNumber"></param>
        public void UpdateTracePaths(SimBodyList simBodyList, ulong iterationNumber)
        {
        }
        private void AddTraceSegment(SimBody simBody)
        {
        }

        /// <summary>
        /// Path elements are rectangular prisims. A single GeometryModel3D is created to be used repeatedly with
        /// different transformations for positioning path elements.
        /// </summary>
        private void InitPathElement()
        {
        }
    }
}
