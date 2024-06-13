using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// The class deals with sending and receiving approach values across thread boundary
    /// </summary>

#if false
    internal struct ApproachElement
    {
        public String Name { get; set; }        // Name of body approached
        public Double CDist { get; set; }       // Closest approach, km-squared
        public Double CSeconds { get; set; }    // Timestamp for closest approach
        public Vector3d CVelocityVec { get; set; }
        public Double FDist { get; set; }       // Furthest approach, km-squared
        public Double FSeconds { get; set; }    // Timestamp for furthest approach
        public Vector3d FVelocityVec { get; set; }
    }
#endif
    internal struct ApproachStatusBody
    {
        public String Name { get; set; }  // Name of body for which approach status applies
        public ApproachElement[] ApproachElements { get; set; }
    }

    // This struct is what is sent in JSON form to the constructor
    internal struct ApproachStatusInfo
    {
        public DateTime DateTime { get; set; } // DT of 0 from start
        public ApproachStatusBody[] ApproachStatusBody { get; set; }

        /// <summary>
        /// Partially initilizes.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dateTime"></param>
        /// <param name="numBodies">Total number of bodies in the system</param>
        /// <remarks>
        /// Space is allocated for the ApproachStatusBody and ApproachElement, but values are not yet initilized.
        /// The number of ApproachStatusBody and ApproachElement allocated is numBodies-1.
        /// </remarks>
        internal ApproachStatusInfo(DateTime dateTime, int numStatusBodies, int numBodies)
        {
            DateTime = dateTime;

            ApproachStatusBody = new ApproachStatusBody[numStatusBodies];

            for (int i = 0; i< numStatusBodies; i++)
                ApproachStatusBody[i].ApproachElements = new ApproachElement[numBodies - 1];
        }
    }

    /// <summary>
    /// Used to communicate approach info to status window
    /// </summary>
    internal class ApproachStatus
    {
        internal ApproachStatusInfo ApproachStatusInfo { get; set; }

        /// <summary>
        /// Instantiate an ApproachStatus, most values to be filled in by caller
        /// </summary>
        /// <param name="name">Of body for which approach info is listed</param>
        /// <param name="dateTime">Sim started</param>
        /// <param name="numStatusBodies">Num bodies for which approach status is provided</param>
        /// <param name="numBodies">in system. There will be numBodies-1 slots for approach info</param>
        public ApproachStatus(DateTime dateTime, int numStatusBodies, int numBodies) 
        {
            ApproachStatusInfo = new(dateTime, numStatusBodies, numBodies);
        }

        /// <summary>
        /// Instantiate an ApproachStatus from JSON representation
        /// </summary>
        /// <param name="aStr"></param>
        public ApproachStatus(String aStr)
        {
            ApproachStatusInfo = JsonSerializer.Deserialize<ApproachStatusInfo>(aStr);
        }

        public String Serialize()
        {
            String aStr = JsonSerializer.Serialize(ApproachStatusInfo);
            return aStr;
        }
    }
}
