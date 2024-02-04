using System;

namespace OrbitalSimOpenGL
{
    public class JPL_Body
    {
        #region Proerties
        public Boolean Selected { get; set; }
        public String ID { get; }
        public String Name { get; }
        public String Designation { get; }
        public String IAU_Alias { get; }
        public String MassStr { get; }
        public String DiameterStr { get; }
        public String GM_Str { get; }
        public String ColorStr { get; }
        #endregion
        
        public JPL_Body(
        bool selected     /* 1 */
            , String id            /* 2 */
            , String name          /* 3 */
            , String designation   /* 4 */
            , String iAU_Alias     /* 5 */
            , String diameteStr    /* 6 */
            , String massStr       /* 7 kg */
            , String gM_Str        /* 8 */
            , String colorStr      /* 9  a Color4 name */
            )
        {
            Selected = selected;
            ID = id;
            Name = name;
            Designation = designation;
            IAU_Alias = iAU_Alias;
            DiameterStr = diameteStr;
            MassStr = massStr;
            GM_Str = gM_Str;
            ColorStr = colorStr;
        }

        public String ToCSV_String()
        {

            // Use,InitSel,ID#,Name,Designation,IAU/aliases/other,Diameter,Mass,GM

            String comma = ",";

            // Bodies.Add(new Body("y".Equals(col[1]), col[2], col[3], col[4], col[5], col[6], col[7], col[8]));
            return new String("y" + comma + (Selected ? "y" : "n") + comma + ID + comma + Name + comma + Designation
                + comma + IAU_Alias + comma + DiameterStr + comma + MassStr + comma + GM_Str + comma + ColorStr);
        }
    }
}
