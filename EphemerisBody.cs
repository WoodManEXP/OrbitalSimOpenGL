using System;
using System.Text.Json.Serialization;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    public class EphemerisBody
    {
        #region Properties
        public String ID { get; set; }
        public String Name { get; set; }
        public String Designation { get; set; }
        public String IAU_Alias { get; set; }
        public String MassStr { get; set; }
        public String DiameterStr { get; set; }
        public String GM_Str { get; set; }
        public String ColorStr { get; set; }

        public String? X_Str { get; set; }
        public String? Y_Str { get; set; }
        public String? Z_Str { get; set; }
        public String? VX_Str { get; set; }
        public String? VY_Str { get; set; }
        public String? VZ_Str { get; set; }
        public String? LT_Str { get; set; }
        public String? RG_Str { get; set; }
        public String? RR_Str { get; set; }
        #endregion

        public EphemerisBody(String id           /* 1 */
                    , String name          /* 2 */
                    , String designation   /* 3 */
                    , String iAU_Alias     /* 4 */
                    , String diameteStr    /* 5 */
                    , String massStr       /* 6 kg */
                    , String gM_Str        /* 7 */
                    , String colorStr
            )
        {
            ID = id;
            Name = name;
            Designation = designation;
            IAU_Alias = iAU_Alias;
            DiameterStr = diameteStr;
            MassStr = massStr;
            GM_Str = gM_Str;
            ColorStr = colorStr;
        }

        [JsonConstructor]
        public EphemerisBody(String id      /* 1 */
                    , String name           /* 2 */
                    , String designation    /* 3 */
                    , String iAU_Alias      /* 4 */
                    , String diameterStr    /* 5 */
                    , String massStr        /* 6 */
                    , String gM_Str
                    , String x_Str, String y_Str, String z_Str
                    , String vX_Str, String vY_Str, String vZ_Str
                    , String lT_Str, String rG_Str, String rR_Str
                    , String colorStr
                    )
        {
            ID = id;
            Name = name;
            Designation = designation;
            IAU_Alias = iAU_Alias;
            DiameterStr = diameterStr;
            MassStr = massStr;
            GM_Str = gM_Str;
            X_Str = x_Str; Y_Str = y_Str; Z_Str = z_Str;
            VX_Str = vX_Str; VY_Str = vY_Str; VZ_Str = vZ_Str;
            LT_Str = lT_Str; RG_Str = rG_Str; RR_Str = rR_Str;
            ColorStr = colorStr;
        }
    }
}
