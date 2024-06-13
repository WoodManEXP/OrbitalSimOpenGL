using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OrbitalSimOpenGL
{
    public struct ApproachElement // One of these at each position in SparseArray
    {
        public String Name { get; set; }        // Name of body approached
        public Double CDist { get; set; }       // Closest approach, km-squared
        public Double CSeconds { get; set; }    // Seconds from start of sim for closest approach
        public Double CVX { get; set; }
        public Double CVY { get; set; }
        public Double CVZ { get; set; }
        public Double FDist { get; set; }       // Furthest approach, km-squared
        public Double FSeconds { get; set; }    // Seconds from start of sim for furthest approach
        public Double FVX { get; set; }
        public Double FVY { get; set; }
        public Double FVZ { get; set; }
    }

    internal class ApproachElements
    {   

        internal ApproachElement[]? Elements { get; set; }

        public ApproachElements(int numSlots) 
        {
            Elements = new ApproachElement[numSlots];
        }
    }
}
