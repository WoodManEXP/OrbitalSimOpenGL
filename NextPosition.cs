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
        private Double Reg_G { get; } = 1E-3 * 6.6743E-11;   // Gravitational constant = N * m^2 / kg^-2 (m s^-2)
                                                             // The 1E-3 converts from kg*m/s-squared to kg*km/s-squared
                                                             // (sim distancs are in km rather than m)
        public int IterationNumber { get; set; } = -1;
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
                //Double simBodyMass = 1E3 * simBody.Mass;
                //Double dV = (forceVector.X * seconds) / simBodyMass;
#if false
                if ("Sun".Equals(simBody.Name))
                {
                    Vector3d speed = new(simBody.VX, simBody.VY, simBody.VZ);
                    System.Diagnostics.Debug.WriteLine("IterateOnce Sun b4:"
                                    + " VX:" + simBody.VX.ToString("0.000000000000E0")
                                    + " VY:" + simBody.VY.ToString("0.000000000000E0")
                                    + " VZ:" + simBody.VZ.ToString("0.000000000000E0")
                                    + " forceVector:" + forceVector.Length.ToString("0.000000000000E0")
                                    + " forceVector X:" + forceVector.X.ToString("0.000000000000E0")
                                    + " forceVector Y:" + forceVector.Y.ToString("0.000000000000E0")
                                    + " forceVector Z:" + forceVector.Z.ToString("0.000000000000E0")
                                    + " speed:" + speed.Length.ToString("0.000000000000E0")
                                    );
                }
#endif
                // New velocity vectors
                simBody.VX += (forceVector.X * seconds) / simBody.Mass;
                simBody.VY += (forceVector.Y * seconds) / simBody.Mass;
                simBody.VZ += (forceVector.Z * seconds) / simBody.Mass;

                Double newX = simBody.X + seconds * ((simBody.VX + simBody.VX) / 2D);
                Double newY = simBody.Y + seconds * ((simBody.VY + simBody.VY) / 2D);
                Double newZ = simBody.Z + seconds * ((simBody.VZ + simBody.VZ) / 2D);

                if (simBody.LastTraceVector3D.X == 0) // Set first RecentVector into the SimBody
                {
                    simBody.LastTraceVector3D.X = newX - simBody.X;
                    simBody.LastTraceVector3D.Y = newY - simBody.Y;
                    simBody.LastTraceVector3D.Z = newZ - simBody.Z;
                }

                // Update body with its new position
                simBody.X = newX;
                simBody.Y = newY;
                simBody.Z = newZ;

#if false
                if ("Jupiter".Equals(simBody.Name) && 0 == IterationNumber % 100)
                {
                    Vector3d speed = new(simBody.VX, simBody.VY, simBody.VZ);
                    System.Diagnostics.Debug.WriteLine("IterateOnce Sun after:"
                                    + "IterationNumber " + IterationNumber.ToString()
                                    + " forceVector X,Y,Z " + forceVector.X.ToString("0.000000000000E0")
                                    + ", " + forceVector.Y.ToString("0.000000000000E0")
                                    + ", " + forceVector.Z.ToString("0.000000000000E0")
                                    + " Len " + forceVector.Length.ToString("0.000000000000E0")
                                    + " VX,VY,VZ " + simBody.VX.ToString("0.000000000000E0")
                                    + ", " + simBody.VY.ToString("0.000000000000E0")
                                    + ", " + simBody.VZ.ToString("0.000000000000E0")
                                    + " Vel " + speed.Length.ToString("0.000000000000E0")
                                    + " X,Y,Z " + simBody.X.ToString("0.000000000000E0")
                                    + ", " + simBody.Y.ToString("0.000000000000E0")
                                    + ", " + simBody.Z.ToString("0.000000000000E0")
                                    );
                }
#endif
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

#if false
            if (0 == bodyNum)
            {
                System.Diagnostics.Debug.WriteLine("SumForceVectors on Sun "
                                                   + " newtons, vec X,Y,Z: "
                                                   + forceVector.Length.ToString("0.000000000000E0")
                                                   + "," + forceVector.X.ToString("0.000000000000E0")
                                                   + "," + forceVector.Y.ToString("0.000000000000E0")
                                                   + "," + forceVector.Z.ToString("0.000000000000E0")
                                                   );
            }
#endif
        }

        /// <summary>
        /// Generate force vectors among bodies givem current state of the SimBodyList
        /// </summary>
        private void CalcForceVectors()
        {
            bool printedSunLoc = false;

            for (int bL = 0; bL < NumBodies; bL++)       // bL - body low number
            {
                SimBody lBody = SimBodyList.BodyList[bL];

                for (int bH = 0; bH < NumBodies; bH++)    // bH - body high number
                {
                    // Nothing for diagonal or below the diagonal entries 
                    if (bH <= bL)
                        continue;

                    SimBody hBody = SimBodyList.BodyList[bH];

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
                    Double newtons = Reg_G * MassMass.GetMassMass(bL, bH) / (d * d);

                    // Each force vector's length is the Newtons of force the pair of bodies exert on one another.
                    ForceVectors[i] *= newtons;
#if false
                    if (0 == bL) // Sun
                    {
                        if (printedSunLoc is false)
                            System.Diagnostics.Debug.WriteLine("CalcForceVectors Sun loc (X,Y,Z) "
                                + lBody.X.ToString("0.000000000000E0")
                                + ", " + lBody.Y.ToString("0.000000000000E0")
                                + ", " + lBody.Z.ToString("0.000000000000E0")
                                );
                        printedSunLoc = true;

                        System.Diagnostics.Debug.WriteLine("CalcForceVectors Sun to " + hBody.Name
                                                                                   + " d (km): " + (d / 1000D).ToString("0.000000000000E0")
                                                                                   + " newtons, vec X,Y,Z: "
                                                                                   + newtons.ToString("0.000000000000E0")
                                                                                   + "," + ForceVectors[i].X.ToString("0.000000000000E0")
                                                                                   + "," + ForceVectors[i].Y.ToString("0.000000000000E0")
                                                                                   + "," + ForceVectors[i].Z.ToString("0.000000000000E0")
                                                                                   );
                        System.Diagnostics.Debug.WriteLine("CalcForceVectors " + hBody.Name + " loc (X, Y, Z) "
                                + hBody.X.ToString("0.000000000000E0")
                                + ", " + hBody.Y.ToString("0.000000000000E0")
                                + ", " + hBody.Z.ToString("0.000000000000E0"));
                    }
#endif
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
