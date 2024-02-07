using OpenTK.Mathematics;
using System;
using System.Windows.Media.Media3D;

namespace OrbitalSimOpenGL
{
    internal class NextPosition
    {
        /// <summary>
        /// Calculates next position of each body.
        /// </summary>
        /// <remarks>
        /// Were the number of bodies in the sim larger, this would be a great class to
        /// segment the model and introduce parallel processing across available cores.
        /// </remarks>
        #region Properties
        private int[] SumOfIntegers { get; set; } // Lookup table so this is not calculated over and over

        // Holds the force vectors for bodies in the system so that is calculated
        // once per iteration instead of twice
        // Nice memory as well as processing/calculation savings - especially as number
        // of bodies increases.
        private Vector3d[] ForceVectors { get; set; }    // Force vectors
        private int NumBodies { get; set; }
        private SimBodyList SimBodyList { get; set; }
        private MassMass MassMass { get; set; }

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
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="simBodyList"></param>
        /// <param name="initialGravConstantSetting">Any initial modification to gravitational constant</param>
        public NextPosition(SimBodyList simBodyList, MassMass massMas, Double initialGravConstantSetting)
        {
            SimBodyList = simBodyList;
            NumBodies = simBodyList.BodyList.Count;

            UseReg_G = initialGravConstantSetting;

            MassMass = massMas;

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
        }

        /// <summary>
        /// Reposition bodies for a single iteration
        /// </summary>
        /// <param name="seconds">Number of seconds elapsed for the iteration</param>
        public void IterateOnce(int seconds)
        {
            SimBody simBody;
            Vector3d forceVector = new(0D, 0D, 0D);

            IterationNumber++;

            CalcForceVectors(); // Between each pair of bodies

            for (int bodyNum = 0; bodyNum < NumBodies; bodyNum++)
            {
                simBody = SimBodyList.BodyList[bodyNum];

                if (simBody.ExcludeFromSim)
                    continue; // No need if bodyNum has been excluded

                SumForceVectors(bodyNum, ref forceVector); // Acting upon this body

                // This force is an acceleration the velocity vectors over the time interval.
                // Calculate new velocity VZ, VY, VZ
                // As derrived from F = ma: dV = (f * dT) / mass-of-body
                // dX, dY, and dZ calculated as average velocity over interval * interval length (seconds)
                // Note the new X, Y, Z are placed in the SimBody after force vector calculation are complete.

                // New velocity vectors
                Double vX = simBody.VX + (forceVector.X * seconds) / simBody.Mass;
                Double vY = simBody.VY + (forceVector.Y * seconds) / simBody.Mass;
                Double vZ = simBody.VZ + (forceVector.Z * seconds) / simBody.Mass;

                Double newX = simBody.X + seconds * ((simBody.VX + simBody.VX) / 2D);
                Double newY = simBody.Y + seconds * ((simBody.VY + simBody.VY) / 2D);
                Double newZ = simBody.Z + seconds * ((simBody.VZ + simBody.VZ) / 2D);

                // Update body to its new position and velocity vectors
                simBody.SetPosAndVel(seconds, newX, newY, newZ, vX, vY, vZ );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bodyNum"></param>
        /// <param name="forceVector"></param>
        /// <remarks>
        /// bodyNum should not have Excluded==true
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
                    Double d = 1E3 * ForceVectors[i].Length; // From km to m
                    ForceVectors[i].Normalize();

                    // Newton's gravational attraction/force calculation
                    Double newtons = UseReg_G * MassMass.GetMassMass(bL, bH) / (d * d);

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
    }
}
