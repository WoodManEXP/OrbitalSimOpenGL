using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

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
        private System.Windows.Point LastMousePosition { get; set; }
        private static int StatIntervalA { get; } = 1000;
        private SimBody? LastMouseOverSB { get; set; } = null;
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

            if ((ElapsedMS += milliseconds) > StatIntervalA)
            {

                OrbitalSimWindow.FPSValue.Content = (1000 / frameRateMS).ToString();

                //                System.Diagnostics.Debug.WriteLine("Stats.Render - interval, frameRateMS " 
                //                    + ElapsedMS.ToString()
                //                    + ", " + frameRateMS.ToString());
                ElapsedMS = 0;

                // Distance to LastMouseOverSB
                if (LastMouseOverSB is not null && SimCamera is not null)
                {
                    Vector3d distVector3D;
                    distVector3D.X = LastMouseOverSB.X - SimCamera.CameraPosition.X;
                    distVector3D.Y = LastMouseOverSB.Y - SimCamera.CameraPosition.Y;
                    distVector3D.Z = LastMouseOverSB.Z - SimCamera.CameraPosition.Z;

                    // Dist from camera position to point on sphere where ray cast would intersect
                    Double dist = distVector3D.Length - (LastMouseOverSB.EphemerisDiameter / 2);

                    OrbitalSimWindow.MouseOverBodyDist.Content = dist.ToString("#,##0") + " km";
                }

            }

            // If mouse over a different body?
            if (LastMouseOverSB != SimModel.LastMouseOverSB)
            {
                LastMouseOverSB = SimModel.LastMouseOverSB;
                if (LastMouseOverSB is not null)
                    OrbitalSimWindow.MouseOverBody.Content = LastMouseOverSB.Name;
            }

            // Mouse position over sim area
            if (LastMousePosition != OrbitalSimWindow.MousePosition)
            {
                OrbitalSimWindow.MouseCoords.Content = 
                    OrbitalSimWindow.MousePosition.X.ToString() + ", " + OrbitalSimWindow.MousePosition.Y.ToString();
                LastMousePosition = OrbitalSimWindow.MousePosition;
            }
        }
    }
}
