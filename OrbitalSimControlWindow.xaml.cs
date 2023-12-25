using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class OrbitalSimControlWindow : Window
    {

        #region Properties
        private bool SimHasBeenStarted { get; set; } = false;
        public JPL_BodyList JPL_BodyList { get; set; }
        public EphemerisBodyList EphemerisBodyList { get; set; }
        public OrbitalSimWindow? OrbitalSimWindow { get; set; }
        private Single OrbitCameraDegrees { get; set; } // Current value on OrbitDegreesSlider
        private Single LookTiltCameraDegrees { get; set; } // Current value on LookTiltDegreesSlider
        private String AppDataFolder { get; set; }
        #endregion

        private OrbitalSimCmds? OrbitalSimCmds { get; set; }

        public OrbitalSimControlWindow()
        {
            var assembly = System.Reflection.Assembly.GetAssembly(this.GetType());//Get the assembly object
            var appName = assembly.GetName().Name;

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AppDataFolder = Path.Combine(localAppData, appName); // appDataFolder = "C:\\Users\\Robert\\AppData\\Local\\OrbitalSimOpenGL"

            InitializeComponent();

            startButton.IsEnabled = true;
            pauseButton.IsEnabled = false;

            // Prep the JPL bodies list
            String initJPL_BodiesFile = Properties.Settings.Default.InitJPL_BodiesFile;
            String savedJPLBodiesFile = Properties.Settings.Default.SavedJPLBodiesFile;
            //String cwdStr = System.IO.Directory.GetCurrentDirectory();

            try
            {
                JPL_BodyList = new JPL_BodyList(initJPL_BodiesFile, savedJPLBodiesFile);
            }
            catch (Exception ex)
            {
                String aMsg = ex.Message + " " + Properties.Settings.Default.NoInitJPL_BodiesCSVFile + ": " + initJPL_BodiesFile;
                MessageBox.Show(aMsg, "Oops");
                Environment.Exit(0);
            }

            // If there is a saved SIM bodies file, load it.
            String savedSimBodiesFile = Properties.Settings.Default.SavedEphemerisBodiesFile;
            String savedSimBodiesPath = Path.Combine(AppDataFolder, savedSimBodiesFile);

            if (File.Exists(savedSimBodiesPath)) // If the file exists
            {
                EphemerisBodyList = EphemerisReader.ReadSavedSimBodies(savedSimBodiesPath);
            }
        }

        // Window loaded
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            // Create the sim window
            //var orbitalSimWindow = new OrbitalSimWindow();
            OrbitalSimWindow = new OrbitalSimWindow();

            OrbitalSimCmds = OrbitalSimWindow.OrbitalSimCmds;

            // Set move camera scale control initial
            MoveScaleSlider.Minimum = 0;
            MoveScaleSlider.Maximum = (Double)Properties.Settings.Default.MaxCamMoveScale - 1;
            MoveScaleSlider.Value = MoveScaleSlider.Maximum / 2;
            MoveScaleLabel.Content = Math.Exp(MoveScaleSlider.Value).ToString("N0") + " km";
            OrbitalSimCmds?.ScaleCamera(MoveScaleSlider.Value);

            // Set orbit slider initial
            OrbitDegreesSlider.Minimum = 1D;
            OrbitDegreesSlider.Maximum = 180D;
            OrbitDegreesSlider.Value = OrbitCameraDegrees = 90F;
            OrbitDegreesLabel.Content = "90";

            // Set Look-Tile slider initial
            LookTiltDegreesSlider.Minimum = 1D;
            LookTiltDegreesSlider.Maximum = 180D;
            LookTiltDegreesSlider.Value = LookTiltCameraDegrees = 10F;
            LookTiltDegreesLabel.Content = "10";

            TimeCompressionLabel.Content = "1 x";

            // Show sim window
            OrbitalSimWindow.Show();

            //System.Diagnostics.Debug.WriteLine("OrbitalSimControlWindow: Window_Loaded ");
        }

        private void CameraLookUp(object sender, RoutedEventArgs e)
        {
            KeepOff();
            OrbitalSimCmds?.LookCamera(SimCamera.CameraLookDirections.LookUp, LookTiltCameraDegrees);
        }

        private void CameraLookLeft(object sender, RoutedEventArgs e)
        {
            KeepOff();
            OrbitalSimCmds?.LookCamera(SimCamera.CameraLookDirections.LookLeft, LookTiltCameraDegrees);
        }

        private void CameraLookRight(object sender, RoutedEventArgs e)
        {
            KeepOff();
            OrbitalSimCmds?.LookCamera(SimCamera.CameraLookDirections.LookRight, LookTiltCameraDegrees);
        }

        private void CameraLookDown(object sender, RoutedEventArgs e)
        {
            KeepOff();
            OrbitalSimCmds?.LookCamera(SimCamera.CameraLookDirections.LookDown, LookTiltCameraDegrees);
        }

        private void CameraTiltCC(object sender, RoutedEventArgs e)
        {
            OrbitalSimCmds?.TiltCamera(SimCamera.CameraTiltDirections.TileCounterClockwise, LookTiltCameraDegrees);
        }

        private void CameraTiltC(object sender, RoutedEventArgs e)
        {
            OrbitalSimCmds?.TiltCamera(SimCamera.CameraTiltDirections.TiltClockwise, LookTiltCameraDegrees);
        }

        private void LookTiltDegreesSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // ... Get Slider reference.
            var slider = sender as Slider;
            // ... Get Value.
            if (slider is not null)
            {
                double value = Math.Floor(slider.Value);
                LookTiltCameraDegrees = (Single)value; // Save and set 
                if (LookTiltDegreesLabel is not null)
                    LookTiltDegreesLabel.Content = value.ToString();
            }
        }
        private void CameraMoveForward(object sender, RoutedEventArgs e)
        {
            OrbitalSimCmds?.MoveCamera(SimCamera.CameraMoveDirections.MoveForward);
        }
        private void CameraMoveBackward(object sender, RoutedEventArgs e)
        {
            OrbitalSimCmds?.MoveCamera(SimCamera.CameraMoveDirections.MoveBackward);
        }
        private void CameraMoveUp(object sender, RoutedEventArgs e)
        {
            OrbitalSimCmds?.MoveCamera(SimCamera.CameraMoveDirections.MoveUp);
        }
        private void CameraMoveLeft(object sender, RoutedEventArgs e)
        {
            OrbitalSimCmds?.MoveCamera(SimCamera.CameraMoveDirections.MoveLeft);
        }
        private void CameraMoveDown(object sender, RoutedEventArgs e)
        {
            OrbitalSimCmds?.MoveCamera(SimCamera.CameraMoveDirections.MoveDown);
        }
        private void CameraMoveRight(object sender, RoutedEventArgs e)
        {
            OrbitalSimCmds?.MoveCamera(SimCamera.CameraMoveDirections.MoveRight);
        }
        private void FileExitMenu(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
        /// <summary>
        /// XtraBodsFileButton
        /// Select JSON file with bodies to add to ephemeris bodies list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XtraBodsButton(object sender, RoutedEventArgs e)
        {

        }
        /// <summary>
        /// BodiesButton
        /// Select bodies from JPL ephemeris to be included in model
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BodiesButton(object sender, RoutedEventArgs e)
        {
            var dialog = new BodiesListDialog(JPL_BodyList);

            bool? result = dialog.ShowDialog();
        }
        private void StartButton(object sender, RoutedEventArgs e)
        {

            EphemerisBodyList ephemerisBodyList = EphemerisBodyList;

            // If no EphemerisBodyList then read the ephemerides from JPL
            if (EphemerisBodyList is null)
            {
                _ = new EphemerisReader(JPL_BodyList, ref ephemerisBodyList);
                EphemerisBodyList = ephemerisBodyList;
            }

            PopulateComboBoxes();

            startButton.IsEnabled = false;
            pauseButton.IsEnabled = true;
            saveBodiesButton.IsEnabled = false;

            OrbitalSimCmds?.StartSim(EphemerisBodyList);

            SimHasBeenStarted = true;
        }

        private void PauseButton(object sender, RoutedEventArgs e)
        {

            startButton.IsEnabled = true;
            startButton.Content = Properties.Settings.Default.ContinueStr;
            pauseButton.IsEnabled = false;
            saveBodiesButton.IsEnabled = true;

            OrbitalSimCmds?.PauseSim();
        }

        /// <summary>
        /// EphemerisButton
        /// Update to most recent ephemeris from JPL
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EphemerisButton(object sender, RoutedEventArgs e)
        {

            // EphemerisReader.ReadSerialized.Serialized will create new BodyList
            // FromJPL uses the passed-in BodyList
            EphemerisBodyList ephemerisBodyList = EphemerisBodyList;

            // Read the ephemerides from JPL
            _ = new EphemerisReader(JPL_BodyList, ref ephemerisBodyList);

            EphemerisBodyList = ephemerisBodyList; // In case it was changed

            //OrbitalSimWindow.BodyList = BodyList;

        }

        private void EditBodiesButton(object sender, RoutedEventArgs e)
        {

        }

        private void SaveBodiesButton(object sender, RoutedEventArgs e)
        {

            // ********* Serialize and save the JPL_BodyList
            String savedBodyList_Path = System.IO.Path.Combine(AppDataFolder, Properties.Settings.Default.SavedEphemerisBodiesFile);
            string jsonString = JsonSerializer.Serialize(EphemerisBodyList);
            File.WriteAllText(savedBodyList_Path, jsonString);
        }

        private void ReadBodiesButton(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void LookAtDropDownOpened(object sender, EventArgs e)
        {
            LookAtComboBox.SelectedItem = null; // In order to be able to select the same entry again
        }

        private void LookAtSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LookAtComboBox.SelectedItem != null)
                OrbitalSimCmds?.LookAtCamera((String)LookAtComboBox.SelectedItem);
        }

        private void GoNearDropDownOpened(object sender, EventArgs e)
        {
            GoNearComboBox.SelectedItem = null; // In order to be able to select the same entry again
        }

        private void GoNearSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GoNearComboBox.SelectedItem != null)
                OrbitalSimCmds?.GoNear((String)GoNearComboBox.SelectedItem);
        }
        private void OrbitAboutDownOpened(object sender, EventArgs e)
        {
            OrbitAboutComboBox.SelectedItem = null; // In order to be able to select the same entry again
        }
        private void OrbitAboutSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrbitAboutComboBox.SelectedItem != null)
                OrbitalSimCmds?.OrbitAbout((String)OrbitAboutComboBox.SelectedItem);
        }
        private void PopulateComboBoxes()
        {

            if (!SimHasBeenStarted)
            {
                // Populate the LookAt, GoNear, and OribitAbout Combobox
                LookAtComboBox.Items.Clear();
                LookAtComboBox.Items.Add(Properties.Settings.Default.Origin);
                LookAtComboBox.Items.Add(Properties.Settings.Default.SystemBarycenter);

                GoNearComboBox.Items.Clear();
                GoNearComboBox.Items.Add(Properties.Settings.Default.Origin);
                GoNearComboBox.Items.Add(Properties.Settings.Default.SystemBarycenter);

                OrbitAboutComboBox.Items.Clear();
                OrbitAboutComboBox.Items.Add(Properties.Settings.Default.Origin);
                OrbitAboutComboBox.Items.Add(Properties.Settings.Default.SystemBarycenter);

                // And an entry for each body in the sim
                foreach (EphemerisBody b in EphemerisBodyList.Bodies)
                {
                    LookAtComboBox.Items.Add(b.Name);
                    GoNearComboBox.Items.Add(b.Name);
                    OrbitAboutComboBox.Items.Add(b.Name);
                }

                LookAtComboBox.SelectedIndex = 0;       // Origin
                OrbitAboutComboBox.SelectedIndex = 0;   // Origin

                // IterationScale combobox
                var lStr = new List<String>() { "1", "2", "5", "10", "30" };
                foreach (String aStr in lStr)
                    IterationScale.Items.Add(aStr);
                IterationScale.SelectedItem = IterationScale.Items.GetItemAt(0);
            }
        }
        private void CameraMoveSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // ... Get Slider reference.
            var slider = sender as Slider;
            // ... Get Value.
            if (slider is not null)
            {
                double value = Math.Floor(slider.Value);
                OrbitalSimCmds?.ScaleCamera(value);
                if (MoveScaleLabel is not null)
                    MoveScaleLabel.Content = Math.Exp(value).ToString("N0") + " km";
            }
        }
        private void OrbitSiderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // ... Get Slider reference.
            var slider = sender as Slider;
            // ... Get Value.
            if (slider is not null)
            {
                double value = Math.Floor(slider.Value);
                OrbitCameraDegrees = (Single)value; // Save and set 
                if (OrbitDegreesLabel is not null)
                    OrbitDegreesLabel.Content = value.ToString();
            }
        }
        private void CameraOrbitUp(object sender, RoutedEventArgs e)
        {
            KeepOff();
            OrbitalSimCmds?.OrbitCamera(SimCamera.CameraOrbitDirections.OrbitUp, OrbitCameraDegrees);
        }
        private void CameraOrbitLeft(object sender, RoutedEventArgs e)
        {
            KeepOff();
            OrbitalSimCmds?.OrbitCamera(SimCamera.CameraOrbitDirections.OrbitLeft, OrbitCameraDegrees);
        }
        private void CameraOrbitDown(object sender, RoutedEventArgs e)
        {
            KeepOff();
            OrbitalSimCmds?.OrbitCamera(SimCamera.CameraOrbitDirections.OrbitDown, OrbitCameraDegrees);
        }
        private void CameraOrbitRight(object sender, RoutedEventArgs e)
        {
            KeepOff();
            OrbitalSimCmds?.OrbitCamera(SimCamera.CameraOrbitDirections.OrbitRight, OrbitCameraDegrees);
        }
        private void IterationUnitsChecked(object sender, RoutedEventArgs e)
        {
            //var radioButton = (RadioButton)sender; // checked RadioButton

            IterationScaleChanged(sender, e as SelectionChangedEventArgs);
        }

        /// <summary>
        /// Set sim iteration rate per frame
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IterationScaleChanged(object sender, SelectionChangedEventArgs e)
        {
            int baseSeconds;
            if (IterateMinutes is not null)
            {
                if ((bool)IterateMinutes.IsChecked)
                    baseSeconds = 60;       // a minute
                else
                    baseSeconds = 60 * 60;  // an hour

                if (IterationScale.SelectedValue is not null) // Happens during app start-up before all controls initialized
                {      
                    // Num seconds to iterate per frame.
                    int scale = int.Parse((String)IterationScale.SelectedValue);

                    // Num seconds will be a number of minutes or number of hours
                    OrbitalSimCmds?.SimIterationRate(scale * baseSeconds);
                }
            }
        }

        /// <summary>
        /// Tracks slider value as slider changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimeCompressionSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // ... Get Slider reference.
            var slider = sender as Slider;
            // ... Get Value.
            if (slider is not null)
            {
                int value = (int)slider.Value;
                if (TimeCompressionLabel is not null)
                    TimeCompressionLabel.Content = value.ToString("N0") + " x";
            }
        }

        /// <summary>
        /// Receives control when changes to time compression slider are complete
        /// https://stackoverflow.com/questions/160995/wpf-slider-doesnt-raise-mouseleftbuttondown-or-mouseleftbuttonup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimeCompressionSliderLostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // ... Get Slider reference.
            var slider = sender as Slider;
            // ... Get Value.
            if (slider is not null)
            {
                int value = (int)slider.Value;
                OrbitalSimCmds.SimTimeCompression(value);
            }
        }

        /// <summary>
        /// Axix checkbox clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AxisCheckbox(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            OrbitalSimCmds.Axis(checkBox.IsChecked.Value);
        }

        /// <summary>
        /// Wireframe checkbox clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WireframeCheckbox(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            OrbitalSimCmds.Wireframe(checkBox.IsChecked.Value);
        }

        /// <summary>
        /// Disable Keep
        /// </summary>
        private void KeepOff()
        {
            KeepCheckbox.IsChecked = false;
            OrbitalSimCmds?.Keep(false);
        }

        /// <summary>
        /// Keep checkbox clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeepClicked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            OrbitalSimCmds.Keep(checkBox.IsChecked.Value);
        }

        private void ReticleCheckbox(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            OrbitalSimCmds.Reticle(checkBox.IsChecked.Value);
        }
    }
}
