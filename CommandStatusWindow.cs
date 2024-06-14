using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OrbitalSimOpenGL.ApproachInfo;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Commands to the Status window
    /// </summary>
    public class CommandStatusWindow : CommandDelegate
    {

        #region Properties

        // Commands
        public enum GenericCommands
        {
              ApproachDistance
            , Reset
        };

        #endregion


        /// <summary>
        /// dispatcher - Dispatch commands to Sim window
        /// </summary>
        /// <param name="dispatcher"></param>
        ///
        public CommandStatusWindow(System.Windows.Threading.Dispatcher dispatcher)
            : base(dispatcher)
        {
        }

        #region ApproachDist
        /// <summary>
        /// Update ApproachDist status area
        /// </summary>
        /// <param name="approachStatusStr">JSON serilization of ApproachDistances.ApproachElements</param>
        /// <param name="names">CSV list of body names/param>
        /// <param name="excludes">CSV list of exclude from sim settings</param>
        public void ApproachDist(String approachStatusStr)
        {
            object[] args = { CommandStatusWindow.GenericCommands.ApproachDistance, approachStatusStr };
            GenericCommand(args);
        }
        #endregion

        #region Reset
        /// <summary>
        /// (Re)set to initial state
        /// </summary>
        public void Reset()
        {
            object[] args = { CommandStatusWindow.GenericCommands.Reset };
            GenericCommand(args);
        }
        #endregion
    }
}
