using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace OrbitalSimOpenGL
{
    public class CommandDelegate
    {
        #region Properties

        protected readonly System.Windows.Threading.Dispatcher Dispatcher;

        #endregion

        /// <summary>
        /// dispatcher - Dispatch commands to Sim window
        /// </summary>
        /// <param name="dispatcher"></param>
        ///
        public CommandDelegate(System.Windows.Threading.Dispatcher dispatcher)
        {
            Dispatcher = dispatcher;
        }

        #region Generic Command

        // Seems easier to use a generic command entry setting parameters for specific commands.
        // Rather than pulling together a delegate for each command.
        public delegate void GenericDelegate(object[] args);
        private GenericDelegate? _GenericDelegate = null;
        public void GenericRegister(GenericDelegate aDelegate)
        {
            _GenericDelegate = aDelegate;
        }
        public void GenericCommand(object[] args)
        {
            if (_GenericDelegate is not null)
                Dispatcher?.BeginInvoke(DispatcherPriority.Normal, _GenericDelegate, args);
        }
        #endregion
    }
}
