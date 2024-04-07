using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

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

        private EphemerisBodyList? _EphemerisBodyList = null;
        public EphemerisBodyList EphemerisBodyList { get => _EphemerisBodyList; set { _EphemerisBodyList = value; } }

        private EphemerisBodyList? _XtraEphemerisBodyList = null;
        public EphemerisBodyList XtraEphemerisBodyList { get => _XtraEphemerisBodyList; set { _XtraEphemerisBodyList = value; } }
        public OrbitalSimWindow? OrbitalSimWindow { get; set; }
        private Single OrbitCameraDegrees { get; set; } // Current value on OrbitDegreesSlider
        private Single LookTiltCameraDegrees { get; set; } // Current value on LookTiltDegreesSlider
        private String AppDataFolder { get; set; }
        #endregion

        private CommandSimWindow? CommandSimWindow { get; set; } // For sending commands to SimWindow
        public CommandControlWindow? OrbitalControlCmds { get; set; }

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
                EphemerisBodyList = EphemerisReader.ReadEphemerisFile(savedSimBodiesPath);
            }
        }

        // Window loaded
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Receives commands for control window
            OrbitalControlCmds = new CommandControlWindow(Dispatcher);

            // Register command delegate(s)
            OrbitalControlCmds.GenericRegister(GenericCommand);

            // Create the sim window
            //var orbitalSimWindow = new OrbitalSimWindow();
            OrbitalSimWindow = new OrbitalSimWindow(OrbitalControlCmds);

            CommandSimWindow = OrbitalSimWindow.OrbitalSimCmds;

            // Set move camera scale control initial
            MoveScaleSlider.Minimum = 0;
            MoveScaleSlider.Maximum = (Double)Properties.Settings.Default.MaxCamMoveScale - 1;
            MoveScaleSlider.Value = MoveScaleSlider.Maximum / 2;
            MoveScaleLabel.Content = Math.Exp(MoveScaleSlider.Value).ToString("N0") + " km";
            CommandSimWindow?.ScaleCamera(MoveScaleSlider.Value);

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

        /// <summary>
        /// Respond to messages coming in over OrbitalControlCmds
        /// </summary>
        /// <param name="args"></param>
        private void GenericCommand(object[] args)
        {
            CommandControlWindow.GenericCommands cmd;

            switch (cmd = (CommandControlWindow.GenericCommands)args[0])
            {
                case CommandControlWindow.GenericCommands.KeepTurnedOff:
                    KeepCombo.SelectedIndex = 0; // Keep was turned off so set KeepCombo to first entry (None)
                    break;

                case CommandControlWindow.GenericCommands.BodyExcluded:
                    BodyExcluded((int)args[1]);
                    break;

                case CommandControlWindow.GenericCommands.BodyRenamed:
                    BodyRenamed((int)args[1], (String)args[2]);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Process a body being excluded from the sim
        /// </summary>
        /// <param name="bodyIndex"></param>
        private void BodyExcluded(int bodyIndex)
        {

            // Once a body is excluded it is out of this sim run, for good.

            // Disable its entry in BodyMods list
            // ItemContainerGenerator.ContainerFromIndex is recommended technique,m but it behaves unpredictably...
            //ListBoxItem lbi = (ListBoxItem)(BodyModsListBox.ItemContainerGenerator.ContainerFromIndex(bodyIndex));
            ListBoxItem lbi = (ListBoxItem)Util.GetByUid(BodyModsListBox, "LBI" + bodyIndex.ToString());
            lbi.IsEnabled = false;

            // Disable in the drop downs
            ((ComboBoxItem)((ItemCollection)LookAtComboBox.Items).GetItemAt(1 + bodyIndex)).IsEnabled = false;
            ((ComboBoxItem)((ItemCollection)GoNearComboBox.Items).GetItemAt(1 + bodyIndex)).IsEnabled = false;
            ((ComboBoxItem)((ItemCollection)OrbitAboutComboBox.Items).GetItemAt(1 + bodyIndex)).IsEnabled = false;
        }

        /// <summary>
        /// Process a body being renamed
        /// </summary>
        /// <param name="bodyIndex"></param>
        private void BodyRenamed(int bodyIndex, String newName)
        {
            // Rename it in BodyMods list

            // Set the label to show value
            Label label = (Label)Util.GetByUid(BodyModsListBox, "Entry" + bodyIndex.ToString());
            if (label is not null)
            {
                String currName = (String)label.Content;
                label.Content = newName;

                // Set Uid in the associated Exclude checkBox to the new name.
                CheckBox checkBox = (CheckBox)Util.GetByUid(BodyModsListBox, "Ex" + currName);
                if (checkBox is not null)
                    checkBox.Uid = "Ex" + newName;

                // Set Uid in the associated Trace checkBox to the new name.
                checkBox = (CheckBox)Util.GetByUid(BodyModsListBox, "Tr" + currName);
                if (checkBox is not null)
                    checkBox.Uid = "Tr" + newName;
            }

            // Change name in the drop downs
            ((ComboBoxItem)((ItemCollection)LookAtComboBox.Items).GetItemAt(1 + bodyIndex)).Content = newName;
            ((ComboBoxItem)((ItemCollection)GoNearComboBox.Items).GetItemAt(1 + bodyIndex)).Content = newName;
            ((ComboBoxItem)((ItemCollection)OrbitAboutComboBox.Items).GetItemAt(1 + bodyIndex)).Content = newName;
        }

        private void CameraLookUp(object sender, RoutedEventArgs e)
        {
            KeepOff();
            CommandSimWindow?.LookCamera(SimCamera.CameraLookDirections.LookUp, LookTiltCameraDegrees);
        }

        private void CameraLookLeft(object sender, RoutedEventArgs e)
        {
            KeepOff();
            CommandSimWindow?.LookCamera(SimCamera.CameraLookDirections.LookLeft, LookTiltCameraDegrees);
        }

        private void CameraLookRight(object sender, RoutedEventArgs e)
        {
            KeepOff();
            CommandSimWindow?.LookCamera(SimCamera.CameraLookDirections.LookRight, LookTiltCameraDegrees);
        }

        private void CameraLookDown(object sender, RoutedEventArgs e)
        {
            KeepOff();
            CommandSimWindow?.LookCamera(SimCamera.CameraLookDirections.LookDown, LookTiltCameraDegrees);
        }

        private void CameraTiltCC(object sender, RoutedEventArgs e)
        {
            CommandSimWindow?.TiltCamera(SimCamera.CameraTiltDirections.TileCounterClockwise, LookTiltCameraDegrees);
        }

        private void CameraTiltC(object sender, RoutedEventArgs e)
        {
            CommandSimWindow?.TiltCamera(SimCamera.CameraTiltDirections.TiltClockwise, LookTiltCameraDegrees);
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
            CommandSimWindow?.MoveCamera(SimCamera.CameraMoveDirections.MoveForward);
        }
        private void CameraMoveBackward(object sender, RoutedEventArgs e)
        {
            CommandSimWindow?.MoveCamera(SimCamera.CameraMoveDirections.MoveBackward);
        }
        private void CameraMoveUp(object sender, RoutedEventArgs e)
        {
            KeepOff();
            CommandSimWindow?.MoveCamera(SimCamera.CameraMoveDirections.MoveUp);
        }
        private void CameraMoveLeft(object sender, RoutedEventArgs e)
        {
            KeepOff();
            CommandSimWindow?.MoveCamera(SimCamera.CameraMoveDirections.MoveLeft);
        }
        private void CameraMoveDown(object sender, RoutedEventArgs e)
        {
            KeepOff();
            CommandSimWindow?.MoveCamera(SimCamera.CameraMoveDirections.MoveDown);
        }
        private void CameraMoveRight(object sender, RoutedEventArgs e)
        {
            KeepOff();
            CommandSimWindow?.MoveCamera(SimCamera.CameraMoveDirections.MoveRight);
        }
        private void FileExitMenu(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
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

        private String LastEphDirectoryName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        /// <summary>
        /// XtraBodsFileButton
        /// Select JSON file with bodies to add to ephemeris bodies list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XtraBodsButton(object sender, RoutedEventArgs e)
        {
            // Get the file path and read with EphemerisBodyList.ReadSavedSimBodies(String pathStr)            
            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Open Xtra Bodies Ephemeris JSON file",
                FileName = "Ephemeris", // Default file name
                DefaultExt = ".json", // Default file extension
                Filter = "JSON documents (.json)|*.json", // Filter files by extension
                InitialDirectory = LastEphDirectoryName
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Remember this for next time
                LastEphDirectoryName = Path.GetDirectoryName(dialog.FileName);

                // If EphemerisBodyList is started add extra bodies to it.
                // Will exclude any added bodies already named in EphemerisBodyList.
                XtraEphemerisBodyList = EphemerisReader.ReadEphemerisFile(dialog.FileName);
            }
        }

        private void StartButton(object sender, RoutedEventArgs e)
        {
            // If no EphemerisBodyList then read the ephemerides from JPL
            if (EphemerisBodyList is null)
            {
                _ = new EphemerisReader(JPL_BodyList, out _EphemerisBodyList);
            }

            // Add in any extra bodies
            // Will exclude any added bodies already named in EphemerisBodyList.
            if (XtraEphemerisBodyList is not null)
                EphemerisBodyList?.Append(XtraEphemerisBodyList);

            PopulateComboBoxes();

            if (SimHasBeenStarted is false)
                PopulateBodyMods();

            startButton.IsEnabled = false;
            pauseButton.IsEnabled = true;
            saveBodiesButton.IsEnabled = false;

            CommandSimWindow?.StartSim(EphemerisBodyList);

            SimHasBeenStarted = true;
        }

        private void PauseButton(object sender, RoutedEventArgs e)
        {

            startButton.IsEnabled = true;
            startButton.Content = Properties.Settings.Default.ContinueStr;
            pauseButton.IsEnabled = false;
            saveBodiesButton.IsEnabled = true;

            CommandSimWindow?.PauseSim();
        }

        /// <summary>
        /// Reset the sim and controls to initial state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetButton(object sender, RoutedEventArgs e)
        {

            bool resetCamera = !NotCamCheckbox.IsChecked.Value;

            // Issue this command prior to OrbitalSimControlWindow reset activities. With OrbitalSimControlWindow
            // and OrbitalSimWindow on separate threads the avtivities will then happen "simultaneously."
            CommandSimWindow?.ResetSim(resetCamera);

            // Reset various elements of OrbitalSimControlWindow
            // Set move camera scale control initial
            MoveScaleSlider.Value = MoveScaleSlider.Maximum / 2;
            MoveScaleLabel.Content = Math.Exp(MoveScaleSlider.Value).ToString("N0") + " km";
            CommandSimWindow?.ScaleCamera(MoveScaleSlider.Value);

            OrbitDegreesSlider.Value = OrbitCameraDegrees = 90F;
            OrbitDegreesLabel.Content = "90";

            LookTiltDegreesSlider.Value = LookTiltCameraDegrees = 10F;
            LookTiltDegreesLabel.Content = "10";

            TimeCompressionSlider.Value = TimeCompressionSlider.Minimum;
            TimeCompressionLabel.Content = "1 x";
            CommandSimWindow?.SimTimeCompression((int)TimeCompressionSlider.Value);
            IterateMinutes.IsChecked = true;
            IterateHours.IsChecked = false;
            IterationScale.SelectedItem = IterationScale.Items.GetItemAt(0);

            GravConstantLabel.Content = "Std";
            GravConstantSlider.Value = 0D;
            CommandSimWindow?.GravConstant(0);

            ShowAxis.IsChecked = ShowReticle.IsChecked = ShowWireframe.IsChecked = ShowBaryCenter.IsChecked = CollisionDetect.IsChecked = true;
            CommandSimWindow?.Axis(true);
            CommandSimWindow?.Wireframe(true);
            CommandSimWindow?.Reticle(true);
            CommandSimWindow?.Barycenter(true);
            CommandSimWindow?.DetectCollisions(true);

            KeepCombo.SelectedIndex = 0;
            LookAtComboBox.SelectedIndex = -1;
            LookAtComboBox.SelectedIndex = 0;       // Origin
            OrbitAboutComboBox.SelectedIndex = 0;   // Origin

            PopulateBodyMods();

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
            // Read the ephemerides from JPL
            _ = new EphemerisReader(JPL_BodyList, out _EphemerisBodyList);
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

        /// <summary>
        /// Select and read an Ephemeris JSON file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// Replaces any EphemerisBodyList that may already be on-board 
        /// </remarks>
        private void ReadBodiesButton(object sender, RoutedEventArgs e)
        {

            // Get the file path and read with EphemerisBodyList.ReadSavedSimBodies(String pathStr)            
            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Open Ephemeris JSON file",
                FileName = "Ephemeris", // Default file name
                DefaultExt = ".json", // Default file extension
                Filter = "JSON documents (.json)|*.json", // Filter files by extension
                InitialDirectory = LastEphDirectoryName
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Remember this for next time
                LastEphDirectoryName = Path.GetDirectoryName(dialog.FileName);

                EphemerisBodyList = EphemerisReader.ReadEphemerisFile(dialog.FileName);
            }
        }

        private void LookAtDropDownOpened(object sender, EventArgs e)
        {
            LookAtComboBox.SelectedItem = null; // In order to be able to select the same entry again
        }

        private void LookAtSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LookAtComboBox.SelectedItem != null)
                CommandSimWindow?.LookAtCamera((String)((ComboBoxItem)LookAtComboBox.SelectedItem).Content);
        }

        private void GoNearDropDownOpened(object sender, EventArgs e)
        {
            GoNearComboBox.SelectedItem = null; // In order to be able to select the same entry again
        }

        private void GoNearSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GoNearComboBox.SelectedItem != null)
                CommandSimWindow?.GoNear((String)((ComboBoxItem)GoNearComboBox.SelectedItem).Content);
        }
        private void OrbitAboutDownOpened(object sender, EventArgs e)
        {
            OrbitAboutComboBox.SelectedItem = null; // In order to be able to select the same entry again
        }
        private void OrbitAboutSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrbitAboutComboBox.SelectedItem != null)
                CommandSimWindow?.OrbitAbout((String)((ComboBoxItem)OrbitAboutComboBox.SelectedItem).Content);
        }
        private void PopulateComboBoxes()
        {
            if (SimHasBeenStarted)
                return;

            // Populate the LookAt, GoNear, and OribitAbout Combobox
            LookAtComboBox.Items.Clear();
            LookAtComboBox.Items.Add(new ComboBoxItem() { Content = Properties.Settings.Default.Origin });
            //LookAtComboBox.Items.Add(Properties.Settings.Default.SystemBarycenter);

            GoNearComboBox.Items.Clear();
            GoNearComboBox.Items.Add(new ComboBoxItem() { Content = Properties.Settings.Default.Origin });
            //GoNearComboBox.Items.Add(Properties.Settings.Default.SystemBarycenter);

            OrbitAboutComboBox.Items.Clear();
            OrbitAboutComboBox.Items.Add(new ComboBoxItem() { Content = Properties.Settings.Default.Origin });
            //OrbitAboutComboBox.Items.Add(Properties.Settings.Default.SystemBarycenter);

            // Add an entry for each body in the sim
            foreach (EphemerisBody b in EphemerisBodyList.Bodies)
            {
                LookAtComboBox.Items.Add(new ComboBoxItem() { Content = b.Name });
                GoNearComboBox.Items.Add(new ComboBoxItem() { Content = b.Name });
                OrbitAboutComboBox.Items.Add(new ComboBoxItem() { Content = b.Name });
            }

            LookAtComboBox.SelectedIndex = 0;       // Origin
            OrbitAboutComboBox.SelectedIndex = 0;   // Origin

            // IterationScale combobox
            var lStr = new List<String>() { "1", "2", "5", "10", "20" };
            foreach (String aStr in lStr)
                IterationScale.Items.Add(aStr);
            IterationScale.SelectedItem = IterationScale.Items.GetItemAt(0);
        }

        private void CameraMoveSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // ... Get Slider reference.
            var slider = sender as Slider;
            // ... Get Value.
            if (slider is not null)
            {
                double value = Math.Floor(slider.Value);
                CommandSimWindow?.ScaleCamera(value);
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
            CommandSimWindow?.OrbitCamera(SimCamera.CameraOrbitDirections.OrbitUp, OrbitCameraDegrees);
        }
        private void CameraOrbitLeft(object sender, RoutedEventArgs e)
        {
            CommandSimWindow?.OrbitCamera(SimCamera.CameraOrbitDirections.OrbitLeft, OrbitCameraDegrees);
        }
        private void CameraOrbitDown(object sender, RoutedEventArgs e)
        {
            CommandSimWindow?.OrbitCamera(SimCamera.CameraOrbitDirections.OrbitDown, OrbitCameraDegrees);
        }
        private void CameraOrbitRight(object sender, RoutedEventArgs e)
        {
            CommandSimWindow?.OrbitCamera(SimCamera.CameraOrbitDirections.OrbitRight, OrbitCameraDegrees);
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
                    CommandSimWindow?.SimIterationRate(scale * baseSeconds);
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
                CommandSimWindow?.SimTimeCompression(value);
            }
        }

        /// <summary>
        /// Axis checkbox clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AxisCheckbox(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (checkBox is not null)
                CommandSimWindow?.Axis(checkBox.IsChecked.Value);
        }

        /// <summary>
        /// Wireframe checkbox clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WireframeCheckbox(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            CommandSimWindow?.Wireframe(checkBox.IsChecked.Value);
        }

        /// <summary>
        /// (Dis)able calcuation and showing of system barycenter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BaryCenterCheckbox(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            CommandSimWindow?.Barycenter(checkBox.IsChecked.Value);
        }

        /// <summary>
        /// (Dis)able collision detection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CollisionCheckbox(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            CommandSimWindow?.DetectCollisions(checkBox.IsChecked.Value);
        }

        /// <summary>
        /// Disable Keep
        /// </summary>
        private void KeepOff()
        {
            //KeepCheckbox.IsChecked = false;
            KeepCombo.SelectedIndex = 0;
            CommandSimWindow?.Keep(SimCamera.KindOfKeep.None);
        }

        private void ReticleCheckbox(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            CommandSimWindow?.Reticle(checkBox.IsChecked.Value);
        }

        /// <summary>
        /// Tell sim what kind of Keep to use
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeepChanged(object sender, SelectionChangedEventArgs e)
        {
            int iSel = KeepCombo.SelectedIndex;

            SimCamera.KindOfKeep keepKind = SimCamera.KindOfKeep.None;

            switch (KeepCombo.SelectedIndex)
            {
                case 0:
                    keepKind = SimCamera.KindOfKeep.None;
                    break;
                case 1:
                    keepKind = SimCamera.KindOfKeep.LookAt;
                    break;
                default:
                    keepKind = SimCamera.KindOfKeep.LookAtAndDistance;
                    break;
            }
            CommandSimWindow?.Keep(keepKind);
        }

        /// <summary>
        /// Tracks slider value as slider changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GravConstantSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // ... Get Slider reference.
            var slider = sender as Slider;
            String aStr;

            // ... Get Value.
            if (slider is not null)
            {
                int value = (int)slider.Value;
                if (0 == value)
                    aStr = "Std";
                else if (0 > value)
                    aStr = "/" + (-value + 1).ToString();
                else
                    aStr = "*" + (value + 1).ToString();

                if (GravConstantLabel is not null)
                    GravConstantLabel.Content = aStr;
            }
        }

        /// <summary>
        /// Receives control when changes to time compression slider are complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GravConstantSliderLostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // ... Get Slider reference.
            var slider = sender as Slider;
            // ... Get Value.
            if (slider is not null)
            {
                int value = (int)slider.Value;

                if (0 > value)
                    value -= 1;
                else if (0 < value)
                    value += 1;

                CommandSimWindow?.GravConstant(value);
            }
        }

        #region BodyModsListBox
        /// <summary>
        /// Prep the BodyMods ListBox
        /// </summary>
        private void PopulateBodyMods()
        {
            BodyModsListBox.Items.Clear();

            int itemIndex = -1;

            // Add an entry for each body in the sim (dynamic buildup of list box contents)
            foreach (EphemerisBody b in EphemerisBodyList.Bodies)
            {
                itemIndex++;

                ListBoxItem listBoxItem = new() { Uid = "LBI" + itemIndex.ToString() };

                StackPanel stackPanel = new() { VerticalAlignment = VerticalAlignment.Center, Orientation = Orientation.Horizontal };
                Label label0 = new() { Content = b.Name, Uid = "Entry" + itemIndex.ToString(), Width = 102, Margin = new(0, 0, 5, 0) };

                CheckBox excludeCheckBox = new() { Uid = "Ex" +  b.Name, VerticalAlignment = VerticalAlignment.Center, Width = 33, Margin = new(0, 0, 10, 0), ToolTip = "Exclude " + b.Name + " from sim" };
                excludeCheckBox.Click += new(BodyModsExcludeCheckbox);

                CheckBox traceCheckBox = new() { Uid = "Tr" + b.Name, VerticalAlignment = VerticalAlignment.Center, Width = 31, Margin = new(0, 0, 10, 0), ToolTip = "Trace " + b.Name + "'s path" };
                traceCheckBox.Click += new(BodyModsTraceCheckbox);

                Slider massSlider = new() { Uid = b.Name, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, ToolTip = "Alter " + b.Name + " +'s mass", Minimum = -9, Maximum = 9, Value = 0, Width = 145 };
                massSlider.LostMouseCapture += new(BodyModsMassSliderLostMouseCapture);
                massSlider.ValueChanged += new(BodyModsMassSliderChanged);

                Label label1 = new() { Uid = b.Name + ":L1", Content = "Std", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Width = 28, Margin = new(2, 0, 5, 0) };

                Slider velSlider = new() { Uid = b.Name, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, ToolTip = "Alter " + b.Name + "'s velocity", Minimum = -9, Maximum = 9, Value = 0, Width = 145 };
                velSlider.LostMouseCapture += new(BodyModsVelocitySliderLostMouseCapture);
                velSlider.ValueChanged += new(BodyModsVelocitySliderChanged);

                Label label2 = new() { Uid = b.Name + ":L2", Content = "Std", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Width = 28, Margin = new(2, 0, 5, 0) };

                stackPanel.Children.Add(label0);
                stackPanel.Children.Add(excludeCheckBox);
                stackPanel.Children.Add(traceCheckBox);
                stackPanel.Children.Add(massSlider);
                stackPanel.Children.Add(label1);
                stackPanel.Children.Add(velSlider);
                stackPanel.Children.Add(label2);

                listBoxItem.Content = stackPanel;

                BodyModsListBox.Items.Add(listBoxItem);
            }
        }

        /// <summary>
        /// Click on Exclude body checkboxes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// Once a body is excluded it cannot be unexcluded
        /// </remarks>
        private void BodyModsExcludeCheckbox(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (checkBox is null)
                return;

            // Send over name of body to exclude
            CommandSimWindow?.ExcludeBody(checkBox.Uid.Substring(2)); // Substring removes the Ex prefix
        }

        private void BodyModsMassSliderLostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // ... Get Slider reference.
            var slider = sender as Slider;
            // ... Get Value.
            if (slider is not null)
            {
                int value = (int)slider.Value;

                if (0 > value)
                    value -= 1;
                else if (0 < value)
                    value += 1;

                // Send of name of body whose mass to change to value
                CommandSimWindow?.MassMultiplier(slider.Uid, value);
            }
        }

        private void BodyModsMassSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // ... Get Slider reference.
            var slider = sender as Slider;
            String aStr;

            // ... Get Value.
            if (slider is not null)
            {
                int value = (int)slider.Value;
                if (0 == value)
                    aStr = "Std";
                else if (0 > value)
                    aStr = "/" + (-value + 1).ToString();
                else
                    aStr = "*" + (value + 1).ToString();

                // Set the label to show value
                Label label = (Label)Util.GetByUid(slider.Parent, slider.Uid + ":L1");
                if (label is not null)
                    label.Content = aStr;
            }
        }

        /// <summary>
        /// Alter current velocity of a body
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BodyModsVelocitySliderLostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // ... Get Slider reference.
            var slider = sender as Slider;
            // ... Get Value.
            if (slider is not null)
            {
                int value = (int)slider.Value;

                if (0 > value)
                    value -= 1;
                else if (0 < value)
                    value += 1;

                // Send of name of body whose velocity to change to value
                CommandSimWindow?.VelocityMultiplier(slider.Uid, value);
            }
        }

        /// <summary>
        /// Change body's velocity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BodyModsVelocitySliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // ... Get Slider reference.
            var slider = sender as Slider;
            String aStr;

            // ... Get Value.
            if (slider is not null)
            {
                int value = (int)slider.Value;
                if (0 == value)
                    aStr = "Std";
                else if (0 > value)
                    aStr = "/" + (-value + 1).ToString();
                else
                    aStr = "*" + (value + 1).ToString();

                // Set the label to show value
                Label label = (Label)Util.GetByUid(slider.Parent, slider.Uid + ":L2");
                if (label is not null)
                    label.Content = aStr;
            }
        }

        /// <summary>
        /// (Dis)able path tracing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BodyModsTraceCheckbox(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (checkBox is null)
                return;

            // Send over name of body to turn on/off tracing
            CommandSimWindow?.TracePath(checkBox.Uid.Substring(2), checkBox.IsChecked.Value); // Substring removes the Tr prefix
        }
    }
    #endregion
}
