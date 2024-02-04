using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Text.Json;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Interaction logic for EphemerisReader.xaml
    /// </summary>
    /// 

    // https://stackoverflow.com/questions/4019831/how-do-you-center-your-main-window-in-wpf
    public partial class EphemerisReader : Window
    {

        #region Properties
        private EphemerisBodyList? EphemerisBodyList { get; set; }
        public string AppDataFolder { get; private set; }
        private JPL_BodyList JPL_BodyList { get; set; }
        private int[] BodiesSelectedFromJPL { get; set; }
        #endregion

        /// <summary>
        /// Construct, by gathering ephemerides from JPL, an EphemerisBodyList given a JPL_BodiesList
        /// </summary>
        /// <param name="jplBodyList">List of bodies for which to gather ephemerides</param>
        /// <param name="ephemerisBodyList">resulting EphemerisBodyList</param>
        public EphemerisReader(JPL_BodyList jplBodyList, ref EphemerisBodyList ephemerisBodyList)
        {

            var assembly = System.Reflection.Assembly.GetAssembly(this.GetType());//Get the assembly object
            var appName = assembly.GetName().Name;

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AppDataFolder = Path.Combine(localAppData, appName); // appDataFolder = "C:\\Users\\Robert\\AppData\\Local\\OrbitalSimWOpenGL"

            JPL_BodyList = jplBodyList;

            // Contact JPL and do the progresss dialog
            InitializeComponent();

            EphemerisBodyList = new EphemerisBodyList();

            BodiesSelectedFromJPL = JPL_BodyList.getSelected();
            int howManySelected = BodiesSelectedFromJPL.GetLength(0);

            progressBar.Value = 0D;
            progressBar.MinHeight = 0D;
            progressBar.Maximum = (double)howManySelected;
            ShowDialog();

            ephemerisBodyList = EphemerisBodyList; // Back to caller
        }

        public void Start()
        {
            System.ComponentModel.BackgroundWorker worker = new()
            {
                WorkerReportsProgress = true
            };
            worker.DoWork += DoWork;
            worker.ProgressChanged += ProgressChanged;
            worker.RunWorkerCompleted += RunWorkerCompleted;

            worker.RunWorkerAsync(progressBar.Maximum);
        }

        private void ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            Label_BodyName.Content = e.UserState;
        }

        // This event handler deals with the results of the
        // background operation.
        private void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // ********* Serialize and save the default EphemerisBodyList as JSON to SavedEphemerisBodiesFile.
            // Subsequent runs can find this for quicker sim startup.
            String pathStr = System.IO.Path.Combine(AppDataFolder, Properties.Settings.Default.SavedEphemerisBodiesFile);
            String jsonString = JsonSerializer.Serialize(EphemerisBodyList);
            File.WriteAllText(pathStr, jsonString);

            this.Close();
        }

        void DoWork(object? sender, DoWorkEventArgs e)
        {

            // DT format: 2021-05-03T23:00:00 yyyy-mm-ddThh:mm:ss (round to hour)
            DateTime dt = DateTime.Now;
            DateTime sDT = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
            DateTime eDT = sDT.AddHours(2);

            String sDT_Str = sDT.ToString("s");
            String eDT_Str = eDT.ToString("s");

            for (int i = 0; i < Convert.ToInt32(e.Argument); i++) // To number of selected bodies
            {

                JPL_Body body = JPL_BodyList.BodyList[BodiesSelectedFromJPL[i]];

                getHorizonsEphemeris(Properties.Settings.Default.HorizonsEphemerisURL, body, sDT_Str, eDT_Str);

                (sender as BackgroundWorker).ReportProgress(1 + i, body.Name);
                Thread.Sleep(100);
            }
        }

        private void getHorizonsEphemeris(String horizonsEphemerisURL, JPL_Body jplBody, String sDT_Str, String eDT_Str)
        {

            horizonsEphemerisURL = horizonsEphemerisURL.Replace("{Command}", jplBody.ID)
                                .Replace("{StartTime}", sDT_Str)
                                .Replace("{StopTime}", eDT_Str);

            WebRequest wrGETURL = WebRequest.Create(horizonsEphemerisURL);

            Stream? objStream;
            try
            {
                objStream = wrGETURL.GetResponse().GetResponseStream();
            }
            catch (Exception e) { return; }

            // Write the file (easier for testing)
            //String savedListCSV_Path = System.IO.Path.Combine(AppDataFolder, Properties.Settings.Default.SavedEphemerisCSVFile);
            //var fileStream = File.Create(savedListCSV_Path);
            //objStream.CopyTo(fileStream);
            //fileStream.Close();

            /*
                  Symbol meaning:

                    0 JDTDB   Julian Day Number
                    1         Calendar Date (TDB) Barycentric Dynamical Time
                    2  X      X-component of position vector (km)
                    3  Y      Y-component of position vector (km)
                    4  Z      Z-component of position vector (km)
                    5  VX     X-component of velocity vector (km/sec)                           
                    6  VY     Y-component of velocity vector (km/sec)                           
                    7  VZ     Z-component of velocity vector (km/sec)                           
                    8  LT     One-way down-leg Newtonian light-time (sec)
                    9  RG     Range; distance from coordinate center (km)
                    10 RR     Range-rate; radial velocity wrt coord. center (km/s

                $$SOE
                data
                $$EOE
            */
            // JDTDB,Calendar Date (TDB),X,Y,Z,VX,VY,VZ,LT,RG,RR,

            StreamReader objReader = new StreamReader(objStream);

            String? sLine;
            String response = new("");
            String? inputLine;

            // Gather the URL response into a String
            sLine = objReader.ReadLine();
            while (sLine != null)
            {
                response = String.Concat(response, sLine + "\n");
                sLine = objReader.ReadLine();
            }

            StringReader stringReader = new(response);

            while ((inputLine = stringReader.ReadLine()) != null)
            {
                if (0 == inputLine.IndexOf("$$SOE")) // If found
                    if ((inputLine = stringReader.ReadLine()) != null)
                    {
                        // Start EphemerisBody with values from csv file
                        EphemerisBody ephemerisBody = new EphemerisBody(
                                                      jplBody.ID            /* 1 */
                                                    , jplBody.Name          /* 2 */
                                                    , jplBody.Designation   /* 3 */
                                                    , jplBody.IAU_Alias     /* 4 */
                                                    , jplBody.DiameterStr   /* 5 */
                                                    , jplBody.MassStr       /* 6 */
                                                    , jplBody.GM_Str        /* 7 */
                                                    , jplBody.ColorStr      /* 8 */
                                                    );

                        String[] values = inputLine.Split(",");

                        // Add ephemeris values from JPL
                        try
                        {
                            ephemerisBody.X_Str = values[2];
                            ephemerisBody.Y_Str = values[3];
                            ephemerisBody.Z_Str = values[4];
                            ephemerisBody.VX_Str = values[5];
                            ephemerisBody.VY_Str = values[6];
                            ephemerisBody.VY_Str = values[6];
                            ephemerisBody.VZ_Str = values[7];
                            ephemerisBody.LT_Str = values[8];
                            ephemerisBody.RG_Str = values[9];
                            ephemerisBody.RR_Str = values[10];

                            EphemerisBodyList.Bodies.Add(ephemerisBody);
                        }
                        catch (Exception e) { }

                        break; // From while loop
                    }
            }

            stringReader.Close();

        }

        private void Loaded(object sender, RoutedEventArgs e)
        {
            Start();
        }

        /// <summary>
        /// Construct an EphemerismBodyList from its JSON file
        /// </summary>
        /// <param name="pathStr"></param>
        /// <returns></returns>
        public static EphemerisBodyList ReadSavedSimBodies(String pathStr)
        {

            // Read the serialized ephemeris
            string jsonString = File.ReadAllText(pathStr);
            EphemerisBodyList ephemerisBodyList = JsonSerializer.Deserialize<EphemerisBodyList>(jsonString)!;
            return ephemerisBodyList;

        }
    }
}
