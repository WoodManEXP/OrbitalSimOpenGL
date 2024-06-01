using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Xml.Linq;

namespace OrbitalSimOpenGL
{
    public class EphemerisBodyList
    {
        #region Properties
        public DateTime EphemerisDateTime { get; set; } // DT for ephemeris gather
        public List<EphemerisBody> Bodies { get; }
        #endregion 

        public EphemerisBodyList()
        {
            Bodies = new List<EphemerisBody>();
        }

        [JsonConstructor]
        public EphemerisBodyList(List<EphemerisBody> bodies)
        {
            Bodies = bodies;
        }

        /// <summary>
        /// Append another to this one
        /// </summary>
        /// <param name="ephemerisBodyList">List to be apended</param>
        /// <remarks>
        /// Will append any with a name not already in the list
        /// </remarks>
        public void Append(EphemerisBodyList appendBodyList)
        {
            // Over each body to be appended
            foreach (EphemerisBody aB in appendBodyList.Bodies)
            {
                bool nameFound = false;
                // Over all bodies already in the list
                foreach (EphemerisBody eB in Bodies)
                {
                    if (aB.Name.Equals(eB.Name))
                    {
                        nameFound = true;
                        break;
                    }
                }
                if (!nameFound)
                    Bodies.Add(aB);
            }
        }
    }
}
