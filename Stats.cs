﻿using OpenTK.Mathematics;
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
        private int ElapsedMS_A { get; set; } = 0;
        private int ElapsedMS_B { get; set; } = 0;
        private System.Windows.Point LastMousePosition { get; set; }
        private static int StatIntervalA { get; } = 1000;
        private static int StatIntervalB { get; } = 30*1000;
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

            // IntervalA stats
            if ((ElapsedMS_A += milliseconds) > StatIntervalA)
            {

                OrbitalSimWindow.FPSValue.Content = (1000 / frameRateMS).ToString();

                //                System.Diagnostics.Debug.WriteLine("Stats.Render - interval, frameRateMS " 
                //                    + ElapsedMS.ToString()
                //                    + ", " + frameRateMS.ToString());
                ElapsedMS_A = 0;

                // Distance to LastMouseOverSB
                if (LastMouseOverSB is not null && SimCamera is not null)
                {
                    Vector3d vector3D;
                    vector3D.X = LastMouseOverSB.X - SimCamera.CameraPosition.X;
                    vector3D.Y = LastMouseOverSB.Y - SimCamera.CameraPosition.Y;
                    vector3D.Z = LastMouseOverSB.Z - SimCamera.CameraPosition.Z;

                    // Dist from camera position to point on sphere where ray cast would intersect
                    Double len = vector3D.Length - (LastMouseOverSB.EphemerisDiameter / 2);

                    // Velocity
                    vector3D.X = LastMouseOverSB.VX;
                    vector3D.Y = LastMouseOverSB.VY;
                    vector3D.Z = LastMouseOverSB.VZ;
                    Double vel = vector3D.Length;

                    // 1 k
                    // m/s = 2236.94 mph
                    String mphStr = "(" + (2236.94D * vel).ToString("#,##0") + " mph)";

                    OrbitalSimWindow.MouseOverBodyDistAndVel.Content = len.ToString("#,##0") + " km, "
                            + ((vel < 1D) ? vel.ToString("#0.###") : vel.ToString("#,##0")) + " km/s "
                            + mphStr;
                }
            }

            // IntervalB stats
            if ((ElapsedMS_B += milliseconds) > StatIntervalA)
            {

                ElapsedMS_B = 0;

                TimeSpan elapsedTime = TimeSpan.FromSeconds(SimModel.ElapsedSeconds);

                int minutes = elapsedTime.Minutes;
                int hours = elapsedTime.Hours;
                int days = elapsedTime.Days;

                Single years = (Single)days / 365.25F;

                OrbitalSimWindow.ElapsedTime.Content = "Elapsed time "
                            + days.ToString("#,##0") + " days "
                            + hours.ToString("#,##0") + " hrs "
                            + minutes.ToString("#,##0") + " mins "
                            + " ~" + years.ToString("#,##0") + " Earth yrs"
                    ;
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
