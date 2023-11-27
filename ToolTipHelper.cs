using System;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// https://stackoverflow.com/questions/38595995/displaying-tooltip-on-mouse-hover-of-a-3d-object

namespace OrbitalSimOpenGL
{
    internal class ToolTipHelper
    {

        private readonly ToolTip _toolTip;
        private readonly Timer _timer;

        /// <summary>
        /// Creates an instance
        /// </summary>
        public ToolTipHelper()
        {
            _toolTip = new ToolTip();
            _timer = new Timer { AutoReset = false };
            _timer.Elapsed += ShowToolTip;
            _timer.Interval = ToolTipService.GetInitialShowDelay(Application.Current.MainWindow);
        }

        /// <summary>
        /// Gets or sets the text for the tooltip.
        /// </summary>
        public object ToolTipContent { get { return _toolTip.Content; } set { _toolTip.Content = value; } }

        public void Show()
        {
            _timer.Start();
        }

        private void ShowToolTip(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            _toolTip?.Dispatcher.Invoke(new Action(() => { _toolTip.IsOpen = true; }));
        }

        /// <summary>
        /// To be called when the mouse leaves the ui area.
        /// </summary>
        public void NoToolTip()
        {
            _timer.Stop();
            if (_toolTip != null)
                _toolTip.IsOpen = false;
        }
    }
}
