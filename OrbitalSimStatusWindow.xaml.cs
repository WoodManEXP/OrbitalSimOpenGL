using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Interaction logic for StatusWindow.xaml
    /// </summary>
    public partial class OrbitalSimStatusWindow : Window
    {
        #region Properties
        public CommandStatuslWindow? CommandStatuslWindow { get; set; }
        #endregion

        public OrbitalSimStatusWindow()
        {
            InitializeComponent();

            // Receives commands for status window
            CommandStatuslWindow = new(Dispatcher);

            // Register command delegate(s)
            CommandStatuslWindow.GenericRegister(GenericCommand);
        }

        // Window loaded
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// Command coming in from somewhere on the message queue
        /// </summary>
        /// <param name="args"></param>
        private void GenericCommand(object[] args)
        {
            switch ((CommandStatuslWindow.GenericCommands)args[0])
            {
                case CommandStatuslWindow.GenericCommands.ApproachDistance:
                    ApproachDistances((String)args[1]);
                    break;

                default:
                    break;
            }
        }

        internal void ApproachDistances(String approachStatusStr)
        {
            ApproachStatus approachStatus = new(approachStatusStr);
        }
    }
}
