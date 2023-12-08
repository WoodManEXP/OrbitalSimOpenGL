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
        //private Double JPL_G { get; } = 6.6743015E-20;    // Gravitational constant km^3 kg^-1 s^-2
        private Double Reg_G { get; } = 6.6743000E-11;    // Gravitational constant N m^2 kg^-2 (m s^-2)
        public ulong IterationNumber { get; set; } = 0;
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="simBodyList"></param>
        public NextPosition(SimBodyList simBodyList)
        {
            SimBodyList = simBodyList;
            NumBodies = simBodyList.BodyList.Count;

            MassMass = new(SimBodyList);

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
        /// Generate force vectors at the current state of the SimBodyList
        /// </summary>
        private void CalcForceVectors()
        {
            for (int bL = 0; bL < NumBodies; bL++)       // bL - body low number
            {
                SimBody lBody = SimBodyList.BodyList[bL];

                for (int bH = 0; bH < NumBodies; bH++)    // bH - body high number
                {
                    // Nothing for diagonal or entries below the diagonal
                    if (bH <= bL)
                        continue;

                    SimBody hBody = SimBodyList.BodyList[bH];

                    // Index of this bL, bH combo into the Vectors table/array.
                    // Same algorithm as used in MassMass.
                    int i = ValuesIndex(bL, bH);

                    // Calc distance between the two bodies and a normalized force
                    // vector from the low body num to the high body num.
                    ForceVectors[i].X = hBody.X - lBody.X;
                    ForceVectors[i].Y = hBody.Y - lBody.Y;
                    ForceVectors[i].Z = hBody.Z - lBody.Z;
                    Double d = 1E3 * ForceVectors[i].Length; // From km to m
                    ForceVectors[i].Normalize();

                    // Newton's gravational attraction calculation
                    Double newtons = Reg_G * MassMass.GetMassMass(bL, bH) / (d * d);

                    // Each force vector's length is proportional to the Newtons of force the pair
                    // of bodies exert on one another.
                    ForceVectors[i] *= newtons;
                }
            }
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
                SumForceVectors(bodyNum, ref forceVector); // Acting upon this body

                // This force is an acceleration the velocity vectors over the time interval.
                // Calculate new velocity VZ, VY, VZ
                // As derrived from F = ma: dV = (f * dT) / mass-of-body
                // dX, dY, and dZ calculated as average velocity over interval * interval length (seconds)
                // New X, Y, Z placed in SimBody elements after calculation are complete.
                simBody = SimBodyList.BodyList[bodyNum];
                Double simBodyMass = 1E3 * simBody.Mass; // Also cvt from m to km
                //Double dV = (forceVector.X * seconds) / simBodyMass;
                Double newVX = simBody.VX + (forceVector.X * seconds) / simBodyMass;
                simBody.VX = newVX;
                Double newVY = simBody.VY + (forceVector.Y * seconds) / simBodyMass;
                simBody.VY = newVY;
                Double newVZ = simBody.VZ + (forceVector.Z * seconds) / simBodyMass;
                simBody.VZ = newVZ;

                simBody.N_X = simBody.X + seconds * ((simBody.VX + newVX) / 2D);
                simBody.N_Y = simBody.Y + seconds * ((simBody.VY + newVY) / 2D);
                simBody.N_Z = simBody.Z + seconds * ((simBody.VZ + newVZ) / 2D);
            }

            // New positions calculated. Update each body with its new position
            for (int bodyNum = 0; bodyNum < NumBodies; bodyNum++)
            {
                simBody = SimBodyList.BodyList[bodyNum];

                if (simBody.LastTraceVector3D.X == 0) // Set first RecentVector into the SimBody
                {
                    simBody.LastTraceVector3D.X = simBody.N_X - simBody.X;
                    simBody.LastTraceVector3D.Y = simBody.N_Y - simBody.Y;
                    simBody.LastTraceVector3D.Z = simBody.N_Z - simBody.Z;
                }

                simBody.X = simBody.N_X;
                simBody.Y = simBody.N_Y;
                simBody.Z = simBody.N_Z;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bodyNum"></param>
        /// <param name="forceVector"></param>
        private void SumForceVectors(int bodyNum, ref Vector3d forceVector)
        {
            forceVector -= forceVector; // To 0

            for (int otherBodyNum = 0; otherBodyNum < NumBodies; otherBodyNum++)
            {
                if (bodyNum != otherBodyNum)
                {
                    // Force vectors between any two bodies originate at body with lower index value.
                    // If bodyNum < otherBodyNum use forceVector as it stands. Otherwise reverse it.
                    int index = ValuesIndex(bodyNum, otherBodyNum);
                    if (bodyNum < otherBodyNum)
                        forceVector += ForceVectors[index];
                    else
                        forceVector -= ForceVectors[index];
                }
            }
        }

        /// <summary>
        /// Generate index into Vectors array
        /// </summary>
        /// <param name="bL">Lower body number - position in SimBodyList/param>
        /// <param name="bH">Higher body number - position in SimBodyList</param>
        /// <returns></returns>
        private int ValuesIndex(int bL, int bH)
        {
            return (bL * NumBodies) - SumOfIntegers[bL] + bH - bL - 1;
        }
    }
}
