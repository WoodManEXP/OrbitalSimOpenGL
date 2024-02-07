﻿using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

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
        public SimCamera SimCamera { get; set; }
        private int ElapsedMS_A { get; set; } = 0;
        private int ElapsedMS_B { get; set; } = 0;
        private System.Windows.Point LastMousePosition { get; set; }
        private static int StatIntervalA { get; } = 1000;
        private static int StatIntervalB { get; } = 30 * 1000;
        private SimBody? ShowStatsForSB { get; set; } = null;

        private Double ClosestApproachDistSquared { get; set; } = Double.MaxValue;
        private int ApproachBodyA { get; set; }
        private int ApproachBodyB { get; set; }
        #endregion

        public Stats(OrbitalSimWindow orbitalSimWindow, SimModel simModel, SimCamera simCamera)
        {
            OrbitalSimWindow = orbitalSimWindow;
            SimModel = simModel;
            SimCamera = simCamera;
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

                // Distance to ShowStatsForSB
                if (ShowStatsForSB is not null && SimCamera is not null)
                {
                    Vector3d position3D = new(ShowStatsForSB.X, ShowStatsForSB.X, ShowStatsForSB.X);

                    Vector3d cameraVector3D = new(position3D.X - SimCamera.CameraPosition.X
                                        , position3D.Y - SimCamera.CameraPosition.Y
                                        , position3D.Z - SimCamera.CameraPosition.Z);

                    // Dist from camera position to point on sphere where ray cast would intersect
                    Double cameraDist = cameraVector3D.Length - ShowStatsForSB.HalfEphemerisDiameter;

                    // Dist from surface of sphere to coordinate system origin.
                    Double originDist = position3D.Length - ShowStatsForSB.HalfEphemerisDiameter;

                    // Velocity
                    cameraVector3D.X = ShowStatsForSB.VX;
                    cameraVector3D.Y = ShowStatsForSB.VY;
                    cameraVector3D.Z = ShowStatsForSB.VZ;
                    Double vel = cameraVector3D.Length;

                    // 1 k
                    // m/s = 2236.94 mph
                    String mphStr = "(" + (2236.94D * vel).ToString("#,##0") + " mph)";

                    OrbitalSimWindow.BodyStats.Content = 
                            "Cam dist " + cameraDist.ToString("#,##0") + " km,"
                            + " orig dist " + originDist.ToString("#,##0") + " km,"
                            + " " + ((vel < 1D) ? vel.ToString("#0.###") : vel.ToString("#,##0")) + " km/s "
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
            if (ShowStatsForSB != SimModel.ShowStatsForSB)
            {
                ShowStatsForSB = SimModel.ShowStatsForSB;
                if (ShowStatsForSB is not null)
                    OrbitalSimWindow.MouseOverBody.Content = ShowStatsForSB.Name;
            }

            // Mouse position over sim area
            if (LastMousePosition != OrbitalSimWindow.MousePosition)
            {
                OrbitalSimWindow.MouseCoords.Content =
                    OrbitalSimWindow.MousePosition.X.ToString() + ", " + OrbitalSimWindow.MousePosition.Y.ToString();
                LastMousePosition = OrbitalSimWindow.MousePosition;
            }

            // Closest approach distance
            if (-1D != SimModel.ClosestApproachDistSquared)
                if (SimModel.ClosestApproachDistSquared < ClosestApproachDistSquared)
                    if (SimModel.SimBodyList is not null) // JIC
                    {
                        // A new closest approach is available
                        ClosestApproachDistSquared = SimModel.ClosestApproachDistSquared;
                        ApproachBodyA = SimModel.ApproachBodyA;
                        ApproachBodyB = SimModel.ApproachBodyB;
                        Double dist = Math.Sqrt(ClosestApproachDistSquared);
                        OrbitalSimWindow.ClosestApproach.Content = dist.ToString("#,##0") + " km, "
                                + SimModel.SimBodyList.BodyList[ApproachBodyA].Name
                                + ", "
                                + SimModel.SimBodyList.BodyList[ApproachBodyB].Name;
                    }
        }
    }
}
