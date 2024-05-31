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
    internal class ApproachElements
    {
        internal struct ApproachElement
        {
            public Double CDist { get; set; }       // Closest approach, km-squared
            public Double CSeconds { get; set; }    // Timestamp for closest approach
            public Double FDist { get; set; }       // Furthest approach, km-squared
            public Double FSeconds { get; set; }    // Timestamp for furthest approach
        }

        internal ApproachElement[]? Elements { get; set; }

        public ApproachElements(int numSlots) 
        {
            Elements = new ApproachElement[numSlots];
        }

        public ApproachElements(String aStr)
        {
            Elements = JsonSerializer.Deserialize<ApproachElement[]>(aStr);
        }

        public String Serialize()
        {
            String aStr = JsonSerializer.Serialize(Elements);

            return aStr;
        }

    }
}
