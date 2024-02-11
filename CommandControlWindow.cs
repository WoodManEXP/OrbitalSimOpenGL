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
            , ExcludeBody
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

        #region Exclude Body
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bodyName"></param>
        public void ExcludeBody(int bodyIndex)
        {
            object[] args = { CommandControlWindow.GenericCommands.ExcludeBody, bodyIndex };
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
