using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Handles stats info display on OrbitalSimWindow
    /// </summary>
    internal class Stats
    {
        #region Properties
        OrbitalSimWindow OrbitalSimWindow { get; set; }
        public SimModel SimModel { get; set; }
        public SimCamera? SimCamera { get; set; }
        private int ElapsedMS { get; set; } = 0;
        private static int StatIntervalA { get; } = 2000;
        #endregion

        public Stats(OrbitalSimWindow orbitalSimWindow, SimModel simModel)
        {
            OrbitalSimWindow = orbitalSimWindow;
            SimModel = simModel;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeSpan">Since last call</param>
        /// <param name="frameRateMS">calculated frameRate</param>
        internal void Render(TimeSpan timeSpan, int frameRateMS)
        {

            int milliseconds = timeSpan.Milliseconds;

            ElapsedMS += milliseconds;

            if ((ElapsedMS += milliseconds) > StatIntervalA)
            {

                OrbitalSimWindow.FPSValue.Content = (1000 / frameRateMS).ToString();

//                System.Diagnostics.Debug.WriteLine("Stats.Render - interval, frameRateMS " 
//                    + ElapsedMS.ToString()
//                    + ", " + frameRateMS.ToString());
                ElapsedMS = 0;
            }

        }
    }
}
