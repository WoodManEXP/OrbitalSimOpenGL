using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Commands to the command window
    /// </summary>
    public class CommandControlWindow : CommandDelegate
    {

        #region Properties

        // Commands
        public enum GenericCommands
        {
            KeepTurnedOff
        };

        #endregion


        /// <summary>
        /// dispatcher - Dispatch commands to Sim window
        /// </summary>
        /// <param name="dispatcher"></param>
        ///
        public CommandControlWindow(System.Windows.Threading.Dispatcher dispatcher)
            : base(dispatcher)
        {
        }

        #region Keep
        public void KeepOff()
        {
            object[] args = { GenericCommands.KeepTurnedOff };
            GenericCommand(args);
        }
        #endregion
    }
}
