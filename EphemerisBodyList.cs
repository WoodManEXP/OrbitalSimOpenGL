using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    public class EphemerisBodyList
    {
        #region Properties
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
    }
}
