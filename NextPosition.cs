﻿using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    internal class NextPosition
    {
        /// <summary>
        /// Calculates next position of each body.
        /// </summary>
        /// <remarks>
        /// Iteratitive calculation
        /// Prior to beginning iterations: Calc & sum FVs, save in LastVectorSum
        /// 
        /// Were the number of bodies in the sim larger, this would be a candidate class to
        /// segment the model and introduce parallel processing across available processors.
        /// </remarks>
        #region Properties
        private int[] SumOfIntegers { get; set; } // Lookup table so this is not calculated over and over

        // Holds the force vectors for bodies in the system so that is calculated
        // once per iteration instead of twice
        // Memory as well as processing/calculation savings - especially as number
        // of bodies increases.
        private Vector3d[] ForceVectors { get; set; }
        private Vector3d[] LastVectorSum { get; set; }
        private Vector3d[] SavedVectorSum { get; set; }
        private Vector3d[] SavedBodyLocation { get; set; }
        private Vector3d[] SavedBodyVelVec { get; set; }
        private int NumBodies { get; set; }
        private SimBodyList SimBodyList { get; set; }
        internal MassMass MassMass { get; set; }

        private Double _UseReg_G = Util.G_KM;
        public Double UseReg_G
        {
            get { return _UseReg_G; }
            set
            {   // <0 divides GC by that value, >0 multiplies GC by that value, 0 sets to std value
                if (0D == value)
                    _UseReg_G = Util.G_KM;
                else if (0D > value)
                    _UseReg_G = Util.G_KM / value;
                else
                    _UseReg_G = Util.G_KM * value;
            }
        }
        public int IterationNumber { get; set; } = -1;
        private Double CosThreshold { get; } = Math.Cos(MathHelper.DegreesToRadians(5E0D));
        private static int MaxSubdivide { get; } = 4;
        private static int BSteps { get; } = 15;
        private Double CoastSeconds { get; set; } = -1D;
        #endregion

        /// <summary>
        /// Repositioning iterations
        /// </summary>
        /// <param name="simBodyList"></param>
        /// <param name="massMass"></param>
        /// <param name="initialGravConstantSetting"></param>
        public NextPosition(SimBodyList simBodyList, MassMass massMass, Double initialGravConstantSetting)
        {
            SimBodyList = simBodyList;
            NumBodies = simBodyList.BodyList.Count;

            UseReg_G = initialGravConstantSetting;

            MassMass = massMass;

            // Construct sum of integers table/array
            SumOfIntegers = new int[NumBodies];
            SumOfIntegers[0] = 0;
            for (int i = 1; i < NumBodies; i++)
                SumOfIntegers[i] = i + SumOfIntegers[i - 1];

            // Construct the Vectors lookup table/array.
            // Could be constructed as a NxN matrix, but the matrix is symmetrical and
            // diagonal values are not needed. So it is made as a 1D array with a
            // function to generate indices into the array. As model size increases
            // this is nice memory/space savings.
            // Number of entries needed is (NumBodies - 1) * NumBodies / 2
            ForceVectors = new Vector3d[(NumBodies - 1) * NumBodies / 2];

            // Keep an iteration's beginning valuse here in case interval must be subdivided
            // (case of FV anle change exceeding RadianThreshold)
            LastVectorSum = new Vector3d[NumBodies];
            SavedVectorSum = new Vector3d[NumBodies];
            SavedBodyLocation = new Vector3d[NumBodies];
            SavedBodyVelVec = new Vector3d[NumBodies];

            // Prior to beginning iterations: Calc & sum FVs, save in LastVectorSum 
            CalcForceVectors(); // Between each pair of bodies
            for (int bodyNum = 0; bodyNum < NumBodies; bodyNum++)
                SumForceVectors(bodyNum, ref LastVectorSum[bodyNum]); // Acting upon this body
        }

        /// <summary>
        /// Reposition bodies for a single iteration
        /// </summary>
        /// <param name="seconds">Target number of seconds to elapse for the iteration.</param>
        /// <remarks>
        ///  The seconds ref param may be changed to a lower number, so be sure to check upon function return.
        /// </remarks>
        public void IterateOnce(ref Double seconds)
        {
            Double minAngleCos;

            IterationNumber++;

#if true
            System.Diagnostics.Debug.WriteLine("\nNextPosition:IterateOnce"
                + " IterationNumber=" + IterationNumber.ToString()
                );
#endif

            // Look out for entering a coasting interval, no accel, from the Degen case
            if (-1D != CoastSeconds)
            {
                seconds = CoastSeconds;
                CoastSeconds = -1D;
            }

            // Attempt to process and interval of requested amount of seconds
            minAngleCos = Subiterate(seconds, true);

#if false

#endif
            if (minAngleCos > CosThreshold) ; // Most common case
            else
            if (minAngleCos == -1D)// 180 degrees
                // The 180/degenerate case. Bodies are approaching one another along the same vector
                // and cross in this interval.
                // Set to run next interval for same period of time so coasting bodies will move
                // to desired location (where FVs balance to save values as beginning of interval).
                // Will sync vectors  symmertrically bac to interval beginning.
                seconds = CoastSeconds = DegenCase(seconds);
            else
                // A larger than comfortable threshold is being crossed. Subdivide the interval to
                // reduce the threshold -> lowers errors in the numerical/incremental methods.
                seconds = NondegenCase(seconds);

#if true
            System.Diagnostics.Debug.WriteLine("NextPosition:IterateOnce"
                + " minAngleCos=" + minAngleCos.ToString()
                + " seconds=" + seconds.ToString()
                + " min angle degrees=" + MathHelper.RadiansToDegrees(Math.Acos(minAngleCos)).ToString()
                );

            for (int bodyNum = 0; bodyNum < NumBodies; bodyNum++)
            {
                SimBody simBody = SimBodyList.BodyList[bodyNum];

                if (simBody.ExcludeFromSim)
                    continue;

                System.Diagnostics.Debug.WriteLine("NextPosition:IterateOnce"
                    + " simBody.Name=" + simBody.Name
                    + " seconds=" + seconds.ToString()
                    + " simBody.X=" + simBody.X.ToString()
                    + " simBody.VX=" + simBody.VX.ToString()
                    + " Beginning FX=" + SavedVectorSum[bodyNum].X.ToString()
                    + " Ending FX=" + LastVectorSum[bodyNum].X.ToString()
                    );
            }
#endif
        }

        private Double Subiterate(Double seconds, bool saveForPossibleRestore)
        {
            SimBody simBody;
            int bodyNum;

            for (bodyNum = 0; bodyNum < NumBodies; bodyNum++)
            {
                simBody = SimBodyList.BodyList[bodyNum];

                if (simBody.ExcludeFromSim)
                    continue; // No need if bodyNum has been excluded

                // This force is an acceleration along the velocity vectors over the time interval.
                // Calculate new velocity VZ, VY, VZ
                // As derrived from F = ma: dV = (f * dT) / mass-of-body
                // a = F / m
                // V at end = v0 + a * t
                // Location  = currLoc + v0 * t + 1/2 * a * t-squared  (Position from velocity and acceleration)
                // Note the new X, Y, Z are placed in the SimBody after force vector calculation are complete.

#if true
                System.Diagnostics.Debug.WriteLine("NextPosition:Subiterate"
                    + " simBody.Name=" + simBody.Name
                    + " simBody.X=" + simBody.X.ToString()
                    + " coasting=" + simBody.CoastThisInterval.ToString()
                    + " seconds=" + seconds.ToString()
                    + " Used VX=" + simBody.VX.ToString()
                );
#endif

                // Begin calculation of new velocity vectors
                Double eVX = simBody.VX;
                Double eVY = simBody.VY;
                Double eVZ = simBody.VZ;

                // Is the body to coast this interval?
                if (simBody.CoastThisInterval)
                    simBody.CoastThisInterval = false;
                else
                {
                    // Not coasting, include acceleration vectors
                    Double aX = LastVectorSum[bodyNum].X / simBody.Mass;
                    Double aY = LastVectorSum[bodyNum].Y / simBody.Mass;
                    Double aZ = LastVectorSum[bodyNum].Z / simBody.Mass;

                    // Add the acceleration
                    eVX += aX * seconds;
                    eVY += aY * seconds;
                    eVZ += aZ * seconds;
#if true
                    System.Diagnostics.Debug.WriteLine("NextPosition:Subiterate"
                        + " simBody.Name=" + simBody.Name
                        + " Used FX=" + LastVectorSum[bodyNum].X.ToString()
                        + " calculated aX=" + aX.ToString()
                        + " calculated eVX=" + eVX.ToString()
                    );
#endif
                }

                // If new vel is over 1/10th c (Does this ever happen ?)
                Vector3d vVec = new(eVX, eVY, eVZ);
                if (Util.C_OneTentSquaredhKMS < vVec.LengthSquared)
                {
                    // Cap the vel vec at C_OneTenthKMS
                    vVec.Normalize();
                    vVec *= Util.C_OneTenthKMS;
                    eVX = vVec.X;
                    eVY = vVec.Y;
                    eVZ = vVec.Z;
                }

                if (saveForPossibleRestore)
                {
                    SavedBodyLocation[bodyNum].X = simBody.X;
                    SavedBodyLocation[bodyNum].Y = simBody.Y;
                    SavedBodyLocation[bodyNum].Z = simBody.Z;
                    SavedBodyVelVec[bodyNum].X = simBody.VX;
                    SavedBodyVelVec[bodyNum].Y = simBody.VY;
                    SavedBodyVelVec[bodyNum].Z = simBody.VZ;
                }

                // Location/displacement at end of interval (Position from initial interval's beginning location and velocity)
                // Note: Use a constant velocity over the interval to calc new positions. Rather than the continuous
                // version of position + (vel * seconds) + (.5 * accel * seconds-squared).
                // Acceleration is approximated by working constant velocity in the intervals, making acceleration
                // within interval unnecessary. Velocity between intervals is ever-changing --> acceleration.
                Double newX = simBody.X + (seconds * eVX);
                Double newY = simBody.Y + (seconds * eVY);
                Double newZ = simBody.Z + (seconds * eVZ);

                simBody.SetPosAndVel(seconds, newX, newY, newZ, eVX, eVY, eVZ); // Most of time these setings will stick
#if true
                System.Diagnostics.Debug.WriteLine("NextPosition:Subiterate"
                    + " simBody.Name=" + simBody.Name
                    + " calculated newX=" + newX.ToString()
#endif
                );

            }

            // Calc FVs at end of pass
            CalcForceVectors(); // Between each pair of bodies

            // Compare the new FV sums with the previous
            Vector3d forceVector;
            Double minAngleCos = 1D;
            for (bodyNum = 0; bodyNum < NumBodies; bodyNum++)
            {
                simBody = SimBodyList.BodyList[bodyNum];

                if (simBody.ExcludeFromSim)
                    continue;

                forceVector = SavedVectorSum[bodyNum] = LastVectorSum[bodyNum];
                SumForceVectors(bodyNum, ref LastVectorSum[bodyNum]); // Sum of FVs acting upon this body

                Double angleCos = CalculateAngleCos(in LastVectorSum[bodyNum], in forceVector);
                minAngleCos = Math.Min(minAngleCos, angleCos);
            }

            return minAngleCos;
        }

        private Double DegenSubiterate(Double seconds)
        {
            SimBody simBody;
            int bodyNum;

            for (bodyNum = 0; bodyNum < NumBodies; bodyNum++)
            {
                simBody = SimBodyList.BodyList[bodyNum];

                if (simBody.ExcludeFromSim)
                    continue; // No need if bodyNum has been excluded

                // This force is an acceleration along the velocity vectors over the time interval.
                // Calculate new velocity VZ, VY, VZ
                // As derrived from F = ma: dV = (f * dT) / mass-of-body
                // a = F / m
                // V at end = v0 + a * t
                // Location  = currLoc + v0 * t + 1/2 * a * t-squared  (Position from velocity and acceleration)
                // Note the new X, Y, Z are placed in the SimBody after force vector calculation are complete.

                // Acceleration vectors
                Double aX = LastVectorSum[bodyNum].X / simBody.Mass;
                Double aY = LastVectorSum[bodyNum].Y / simBody.Mass;
                Double aZ = LastVectorSum[bodyNum].Z / simBody.Mass;

                // New velocity vectors
                Double eVX = simBody.VX + aX * seconds;
                Double eVY = simBody.VY + aY * seconds;
                Double eVZ = simBody.VZ + aZ * seconds;

                // If new vel is over 1/10th c (Does this ever happen ?)
                Vector3d vVec = new(eVX, eVY, eVZ);
                if (Util.C_OneTentSquaredhKMS < vVec.LengthSquared)
                {
                    // Cap the vel vec at C_OneTenthKMS
                    vVec.Normalize();
                    vVec *= Util.C_OneTenthKMS;
                    eVX = vVec.X;
                    eVY = vVec.Y;
                    eVZ = vVec.Z;
                }

                // Location/displacement at end of interval (Position from initial interval's beginning location and velocity)
                // Note: Use a constant velocity over the interval to calc new positions. Rather than the continuous
                // version of position + (vel * seconds) + (.5 * accel * seconds-squared).
                // Acceleration is approximated by working constant velocity in the intervals, making acceleration
                // within interval unnecessary. Velocity between intervals is ever-changing --> acceleration.
                Double newX = simBody.X + (seconds * eVX);
                Double newY = simBody.Y + (seconds * eVY);
                Double newZ = simBody.Z + (seconds * eVZ);

                simBody.SetPosAndVel(seconds, newX, newY, newZ, eVX, eVY, eVZ);
            }

            // Calc FVs at end of pass
            CalcForceVectors(); // Between each pair of bodies

            // Compare the new FV sums with the previous
            Vector3d forceVector;
            Double minAngleCos = 1D;
            for (bodyNum = 0; bodyNum < NumBodies; bodyNum++)
            {
                simBody = SimBodyList.BodyList[bodyNum];

                if (simBody.ExcludeFromSim)
                    continue;

                forceVector = SavedVectorSum[bodyNum] = LastVectorSum[bodyNum];
                SumForceVectors(bodyNum, ref LastVectorSum[bodyNum]); // Sum of FVs acting upon this body

                Double angleCos = CalculateAngleCos(in LastVectorSum[bodyNum], in forceVector);
                minAngleCos = Math.Min(minAngleCos, angleCos);

                // If this body is involved in the 180 degree/degenerate case, mark it
                if (-1D == angleCos)
                    simBody.CoastThisInterval = true;
            }

            return minAngleCos;
        }

        private Double NondegenCase(Double seconds)
        {
            SimBody simBody;
            Double minAngleCos;
            int bodyNum;

            // Subdivide interval till minAngleCos rises above CosThreshold
            for (int subdivide = 0; subdivide < MaxSubdivide; subdivide++)
            {
                Restore();// Restore values from beginning of interval

                seconds /= 2D;
                minAngleCos = Subiterate(seconds, false);
#if false
                    System.Diagnostics.Debug.WriteLine("NextPosition:IterateOnce"
                        + " subdivide= " + subdivide.ToString()
                        + " IterationNumber=" + IterationNumber.ToString()
                        + " CosThreshold=" + CosThreshold.ToString()
                        + " minAngleCos=" + minAngleCos.ToString()
                        + " max angle degrees=" + MathHelper.RadiansToDegrees(Math.Acos(minAngleCos)).ToString()
                        );
#endif
                if (minAngleCos > CosThreshold)
                    break;
            }
            return seconds;
        }

        /// <summary>
        /// Setup to process a degenerate interval (180 degree FV flip)
        /// </summary>
        /// <returns>
        /// Approximation of seconds value where flip occurs
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        private double DegenCase(Double seconds)
        {
            Double minAngleCos = 1D;

            // Binary search the interval to develop an estimate of where the vector flips
            seconds /= 2D;
            Double halfInterval = seconds;
            for (int i = 0; i < BSteps; i++)
            {
                Restore(); // Restore values from beginning of interval

                minAngleCos = DegenSubiterate(seconds);

                halfInterval /= 2D;
                if (-1D == minAngleCos) // 180 degrees
                    seconds -= halfInterval; // Flip encountered, so backup
                else
                    seconds += halfInterval;
            }

#if false
            System.Diagnostics.Debug.WriteLine("NextPosition:DegenCase"
                );
#endif
            // Here an approximation has been made for where the FV flips 180 degrees
            // The +halfInterval ensures approximation is on other side of flip - so
            // that on next interval this flip will not be encountered.
            if (-1D == minAngleCos)
                seconds += halfInterval;    // Advanve time just past flip

            Restore();
            minAngleCos = DegenSubiterate(seconds);   // Move over the flip

            return seconds;
        }

        /// <summary>
        /// Back to original values from beginning of interval
        /// </summary>
        private void Restore()
        {
            for (int bodyNum = 0; bodyNum < NumBodies; bodyNum++)
            {
                SimBody simBody = SimBodyList.BodyList[bodyNum];
                if (simBody.ExcludeFromSim)
                    continue;

                LastVectorSum[bodyNum] = SavedVectorSum[bodyNum];
                simBody.X = SavedBodyLocation[bodyNum].X;
                simBody.Y = SavedBodyLocation[bodyNum].Y;
                simBody.Z = SavedBodyLocation[bodyNum].Z;
                simBody.VX = SavedBodyVelVec[bodyNum].X;
                simBody.VY = SavedBodyVelVec[bodyNum].Y;
                simBody.VZ = SavedBodyVelVec[bodyNum].Z;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bodyNum"></param>
        /// <param name="forceVector"></param>
        /// <remarks>
        /// FVs represent Newtons of force in each dimension.
        /// bodyNum should not refer to an excluded body.
        /// </remarks>
        private void SumForceVectors(int bodyNum, ref Vector3d forceVector)
        {
            forceVector -= forceVector; // To 0

            for (int otherBodyNum = 0; otherBodyNum < NumBodies; otherBodyNum++)
            {
                // No need if otherBodyNum has been excluded
                if (SimBodyList.BodyList[otherBodyNum].ExcludeFromSim)
                    continue;

                if (bodyNum != otherBodyNum)
                {
                    // ValuesIndex indices are generated by bodyNum pairs.
                    // Force vectors between any two bodies represent attraction from body with lower index to
                    // body with higher index, attraction of lower to higher (bL ---> bH)
                    int index = ValuesIndex(bodyNum, otherBodyNum);
                    if (bodyNum < otherBodyNum)
                        forceVector += ForceVectors[index];
                    else
                        forceVector -= ForceVectors[index];
                }
            }
        }

        /// <summary>
        /// Generate force vectors among bodies given current state of the SimBodyList
        /// </summary>
        /// <remarks>
        /// FVs represent Newtons of force in each dimension
        /// </remarks>
        private void CalcForceVectors()
        {
            for (int bL = 0; bL < NumBodies; bL++)       // bL - body low number
            {
                SimBody lBody = SimBodyList.BodyList[bL];

                if (lBody.ExcludeFromSim)
                    continue; // No need if lBody has been excluded

                for (int bH = 0; bH < NumBodies; bH++)    // bH - body high number
                {
                    // Nothing for diagonal or below the diagonal entries 
                    if (bH <= bL)
                        continue;

                    SimBody hBody = SimBodyList.BodyList[bH];

                    if (hBody.ExcludeFromSim)
                        continue; // No need if hBody has been excluded

                    // Index of this bL, bH combo into the Vectors table/array.
                    // Same algorithm as used in MassMass.
                    int i = ValuesIndex(bL, bH);

                    // Calc normalized vector between two bodies.
                    // Force vectors between any two bodies represent attraction from body with lower index to
                    // body with higher index, attraction of lower to higher (bL ---> bH)
                    ForceVectors[i].X = hBody.X - lBody.X;
                    ForceVectors[i].Y = hBody.Y - lBody.Y;
                    ForceVectors[i].Z = hBody.Z - lBody.Z;

                    Double dSquared = 1E6 * ForceVectors[i].LengthSquared; // From km to m
                    ForceVectors[i].Normalize();

                    // Newton's gravational attraction/force calculation
                    Double newtons = UseReg_G * MassMass.GetMassMass(bL, bH) / dSquared;

                    // Each force vector's length is the Newtons of force the pair of bodies exert on one another.
                    ForceVectors[i] *= newtons;
                }
            }
        }

        /// <summary>
        /// Generate index into Vectors array
        /// </summary>
        /// <param name="body0">One body number - position in SimBodyList/param>
        /// <param name="body1">Other body number - position in SimBodyList</param>
        /// <returns></returns>
        private int ValuesIndex(int body0, int body1)
        {
            var lBL = Math.Min(body0, body1);
            var lBh = Math.Max(body0, body1);
            return (lBL * NumBodies) - SumOfIntegers[lBL] + lBh - lBL - 1;
        }

        /// <summary>
        /// Calc Cos of angle between two Vector3ds
        /// Use this rather than the Vector3d version. More efficient.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <remarks>
        /// No need to do the acos call, as in Vector3d version, to obtain a usable result
        /// </remarks>
        /// <returns></returns>
        private static Double CalculateAngleCos(in Vector3d first, in Vector3d second)
        {
            Vector3d.Dot(in first, in second, out var result2);
            Double result = MathHelper.Clamp(result2 / (first.Length * second.Length), -1.0, 1.0);
            return result;
        }
    }
}
