using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Manages visuals for 
    /// - Highlighting
    /// - Collisions/Explosions
    /// </summary>
    internal class Highlighter
    {
        #region Properties

        // Highlight types
        public enum HighlightType
        {
            Highlight
          , Collision
        };

        public HighlightType HType;

        #endregion

        public Highlighter(HighlightType hType) 
        {
            HType = hType;
        }
    }
}
