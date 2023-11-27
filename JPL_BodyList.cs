using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Represents the list of bodies for which emhemerides may be gathered from JPJ Horizons
    /// </summary>
    public class JPL_BodyList
    {
        #region Properties

        public String SavedJPLBodiesFile { get; set; }
        public String InitJPL_BodiesFile { get; set; }
        public List<JPL_Body> BodyList { get; set; }
        private String AppDataFolder { get; set; }

        #endregion
        public JPL_BodyList(String initJPL_BodiesFile, String savedJPLBodiesFile)
        {

            SavedJPLBodiesFile = savedJPLBodiesFile;
            InitJPL_BodiesFile = initJPL_BodiesFile;

            BodyList = new List<JPL_Body>();

            // https://stackoverflow.com/questions/653128/how-to-get-namespace-of-an-assembly
            var assembly = System.Reflection.Assembly.GetAssembly(this.GetType());//Get the assembly object
            var appName = assembly.GetName().Name;

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AppDataFolder = Path.Combine(localAppData, appName); // appDataFolder = "C:\\Users\\Robert\\AppData\\Local\\OrbitalSimWPF2"

            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }

            String savedJPL_Bodies_Path = Path.Combine(AppDataFolder, savedJPLBodiesFile);

            // If the saved list file exists
            if (File.Exists(savedJPL_Bodies_Path))
            {
                LoadList(false);
            }
            else
            {
                LoadList(true);
            }
        }

        public void LoadList(bool initialLoad)
        {

            String csvFileName = initialLoad ? InitJPL_BodiesFile : SavedJPLBodiesFile;

            // Use,InitSel,ID#,Name,Designation,IAU/aliases/other,Diameter,Mass,GM

            BodyList.Clear();

            // path to the csv file
            String csvPath = Path.Combine(AppDataFolder, csvFileName);

            String[] csvBodies = System.IO.File.ReadAllLines(csvPath);
            foreach (String row in csvBodies)
            {
                String[] col = row.Split(',');

                if ("y".Equals(col[0])) // Entries with "y" here are available for sim (Has the effect of ignoring the header line)
                    BodyList.Add(new JPL_Body("y".Equals(col[1]), col[2], col[3], col[4], col[5], col[6], col[7], col[8]));
            }
        }

        internal int HowManySelected()
        {
            int i = 0;
            foreach (JPL_Body b in BodyList)
                if (b.Selected)
                    i++;
            return i;
        }
        /// <summary>
        /// Write the BodyList in CSV format to SavedJPLBodiesFile
        /// </summary>
        public void SaveBodyList()
        {
            // path to the csv file
            String saveCSV_Path = Path.Combine(AppDataFolder, SavedJPLBodiesFile);

            try
            {
                var writer = new StreamWriter(saveCSV_Path);

                writer.WriteLine("Use,InitSel,ID#,Name,Designation,IAU/aliases/other,Diameter,Mass,GM");

                foreach (JPL_Body b in BodyList)
                {
                    writer.WriteLine(b.ToCSV_String());
                }
                writer.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Oops");
            }
        }

        public void SelectAll(Boolean bVal)
        {
            foreach (JPL_Body b in BodyList)
                b.Selected = bVal;
        }

        public void SetSelected(int n, Boolean selected)
        {
            BodyList.ElementAt(n).Selected = selected;
        }

        public int[] getSelected()
        {
            int[] selected = new int[HowManySelected()];

            int index = -1, i = -1;
            foreach (JPL_Body b in BodyList)
            {
                i++;
                if (b.Selected)
                    selected[++index] = i;
            }
            return selected;

        }
    }
}
