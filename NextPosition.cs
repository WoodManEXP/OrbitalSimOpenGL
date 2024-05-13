using OpenTK.Graphics.OpenGL;
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
        private SparseArray SparseArray { get; set; } // Lookup table so this is not calculated over and over

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
        private Double CosThreshold { get; } = Math.Cos(MathHelper.DegreesToRadians(10E0D));
        private static int MaxSubdivide { get; } = 4;
        private static int BSteps { get; } = 15;
        #endregion

        /// <summary>
        /// Repositioning iterations
        /// </summary>
        /// <param name="simBodyList"></param>
        /// <param name="massMass"></param>
        /// <param name="initialGravConstantSetting"></param>
        public NextPosition(SimBodyList simBodyList, SparseArray sparseArray, MassMass massMass, Double initialGravConstantSetting)
        {
            SimBodyList = simBodyList;
            NumBodies = simBodyList.BodyList.Count;

            UseReg_G = initialGravConstantSetting;

            MassMass = massMass;
            SparseArray = sparseArray;

            // Construct the Vectors lookup table/array.
            // Could be constructed as a NxN matrix, but the matrix is symmetrical and
            // diagonal values are not needed. So it is made as a 1D array with a
            // function to generate indices into the array. As model size increases
            // this is nice memory/space savings.
            // Number of entries needed is (NumBodies - 1) * NumBodies / 2
            ForceVectors = new Vector3d[SparseArray.NumSlots/*(NumBodies - 1) * NumBodies / 2*/];

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
        ///  The seconds ref param may be changed to a lower number, so be sure to check upon method return.
        /// </remarks>
        public void IterateOnce(ref Double seconds)
        {
            Double minAngleCos;

            IterationNumber++;

            // Attempt to process and interval of requested amount of seconds
            minAngleCos = Subiterate(seconds, true);

#if false
            System.Diagnostics.Debug.WriteLine("\nNextPosition:IterateOnce"
                + " IterationNumber=" + IterationNumber.ToString()
                + " min angle degrees=" + MathHelper.RadiansToDegrees(Math.Acos(minAngleCos)).ToString()
                );
#endif

            if (true/*minAngleCos > CosThreshold*/) ; // Most common case
            else
            if (minAngleCos == -1D)// 180 degrees
                // The 180/degenerate case. Bodies are approaching one another along the same vector
                // and cross in this interval.
                // Set to run next interval for same period of time so coasting bodies will move
                // to desired location (where FVs balance to save values as beginning of interval).
                // Will sync vectors  symmertrically bac to interval beginning.
                DegenCase(ref seconds);
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

                // New velocity vectors
                Double eVX = simBody.VX + ((LastVectorSum[bodyNum].X * seconds) / simBody.Mass);
                Double eVY = simBody.VY + ((LastVectorSum[bodyNum].Y * seconds) / simBody.Mass);
                Double eVZ = simBody.VZ + ((LastVectorSum[bodyNum].Z * seconds) / simBody.Mass);

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

                Double newX = simBody.X + seconds * eVX;
                Double newY = simBody.Y + seconds * eVY;
                Double newZ = simBody.Z + seconds * eVZ;

                if (saveForPossibleRestore)
                {
                    SavedBodyLocation[bodyNum].X = simBody.X;
                    SavedBodyLocation[bodyNum].Y = simBody.Y;
                    SavedBodyLocation[bodyNum].Z = simBody.Z;
                    SavedBodyVelVec[bodyNum].X = simBody.VX;
                    SavedBodyVelVec[bodyNum].Y = simBody.VY;
                    SavedBodyVelVec[bodyNum].Z = simBody.VZ;
                }

                simBody.SetPosAndVel(seconds, newX, newY, newZ, eVX, eVY, eVZ); // Most of time these setings will stick
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

                // Perhaps change this to check for parallel but opposite FV's.
                // Should be less computation.
                //
                Double angleCos = CalculateAngleCos(in LastVectorSum[bodyNum], in forceVector);
                minAngleCos = Math.Min(minAngleCos, angleCos);
            }

            return minAngleCos;
        }

        /// <summary>
        /// Handle the 180 degree flip case
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        private Double DegenSubiterate(Double seconds)
        {
            SimBody simBody;
            int bodyNum;

            for (bodyNum = 0; bodyNum < NumBodies; bodyNum++)
            {
                simBody = SimBodyList.BodyList[bodyNum];

                if (simBody.ExcludeFromSim)
                    continue; // No need if bodyNum has been excluded

                // New velocity vectors
                Double eVX = simBody.VX + ((LastVectorSum[bodyNum].X * seconds) / simBody.Mass);
                Double eVY = simBody.VY + ((LastVectorSum[bodyNum].Y * seconds) / simBody.Mass);
                Double eVZ = simBody.VZ + ((LastVectorSum[bodyNum].Z * seconds) / simBody.Mass);

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

                Double newX = simBody.X + seconds * eVX;
                Double newY = simBody.Y + seconds * eVY;
                Double newZ = simBody.Z + seconds * eVZ;

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

        /// <summary>
        /// Setup to process a degenerate interval (180 degree FV flip)
        /// </summary>
        /// <returns>
        /// Approximation of seconds value where flip occurs
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        private void DegenCase(ref Double seconds)
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

            // Here an approximation has been made for where the FV flips 180 degrees
            // The +halfInterval ensures approximation is on other side of flip - so
            // that on next interval this flip will not be encountered.
            if (-1D == minAngleCos)
                seconds += halfInterval;    // Advanve time just past flip

            Restore();
            minAngleCos = DegenSubiterate(seconds);   // Move over the flip
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
                    int index = SparseArray.ValuesIndex(bodyNum, otherBodyNum);
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
                    int i = SparseArray.ValuesIndex(bL, bH);

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
