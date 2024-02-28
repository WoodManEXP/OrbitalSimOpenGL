using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Renders collision highlights
    /// </summary>
    internal class CollisionHighlighter
    {

        #region Properties
        internal SimBody SimBody { get; set; }
        internal Scale? Scale { get; set; }
        private bool Highlighting { get; set; } = false;
        private int MS_SoFar { get; set; } = 0;
        #endregion

        /// <summary>
        /// Renders collision highlights
        /// </summary>
        /// <param name="simBody">To be highlighted</param>
        internal CollisionHighlighter(SimBody simBody)
        {
            SimBody = simBody;
            Scale = simBody.Scale;
        }

        internal void Render(int ms)
        {
            if (!Highlighting)
                return;

            MS_SoFar += ms;

        }
    }
}
