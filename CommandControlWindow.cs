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
            , BodyExcluded
            , BodyRenamed
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

        #region Body Excluded
        /// <summary>
        /// A body has been excluded
        /// </summary>
        /// <param name="bodyIndex"></param>
        public void BodyExcluded(int bodyIndex)
        {
            object[] args = { CommandControlWindow.GenericCommands.BodyExcluded, bodyIndex };
            GenericCommand(args);
        }
        #endregion

        #region Body Renamed
        public void BodyRenamed(int bodyIndex, String bodyName)
        {
            object[] args = { CommandControlWindow.GenericCommands.BodyRenamed, bodyIndex, bodyName };
            GenericCommand(args);
        }
        #endregion
    }
}
