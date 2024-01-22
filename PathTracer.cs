using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Provides visual path tracing of the bodies in the 3D space
    /// </summary>
    internal class PathTracer
    {
        #region Properties
        private Scale Scale; // For universe to WPF coords
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathTraceModelVisual3D">The SimModelVisual3D into which to place the path elements</param>
        public PathTracer(Scale scale)
        {
            Scale = scale;
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
