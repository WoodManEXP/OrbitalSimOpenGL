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
        /// Iteratitive calculation
        /// Prior to beginning iterations: Calc & sum FVs, save in LastVectorSum
        /// 
        /// Iterations:
        /// 1. Calc body's velocity at next location using saved FV sum
        /// 2. Reposition body along the FV using avg(current vel and vel at next location)
        /// 3. Calc and resave FVB sum vectors at this next location
        /// 
        /// 
        /// This is a poor-man's integration technique. FV's change continuously across the interval
        /// so an average can be used as a better, but not perfect, approximation. Effects are expecially appreciated when 
        /// bodies pass close by or even through one another, no collision detection, causing a significant change to the FV
        /// during the interval.
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
            LastVectorSum = new Vector3d[NumBodies];

            // Prior to beginning iterations: Calc & sum FVs, save in LastVectorSum 
            CalcForceVectors(); // Between each pair of bodies
            for (int bodyNum = 0; bodyNum < NumBodies; bodyNum++)
                SumForceVectors(bodyNum, ref LastVectorSum[bodyNum]); // Acting upon this body
        }

        /// <summary>
        /// Reposition bodies for a single iteration
        /// </summary>
        /// <param name="seconds">Number of seconds elapsed for the iteration</param>
        /// <param name="secondsSquared">Number of seconds-squared elapsed for the iteration</param>
        public void IterateOnce(int seconds, int secondsSquared)
        {
            SimBody simBody;

            IterationNumber++;

#if false
            System.Diagnostics.Debug.WriteLine("NextPosition:IterateOnce");
#endif

            // 1. Calc body's next location using saved FV sum
            // New location calculated as average velocity over interval = (beginning V + ending V) / 2
            for (int bodyNum = 0; bodyNum < NumBodies; bodyNum++)
            {
                simBody = SimBodyList.BodyList[bodyNum];

                if (simBody.ExcludeFromSim)
                    continue; // No need if bodyNum has been excluded

                // This force is an acceleration along the velocity vectors over the time interval.
                // Calculate new velocity VZ, VY, VZ
                // As derrived from F = ma: dV = (f * dT) / mass-of-body
                // a = F / m
                // V at end = v0 + a * t
                // Location  = currLoc + v0 * t + 1/2 *a * t-squared  (Position from velocity and acceleration)
                // Note the new X, Y, Z are placed in the SimBody after force vector calculation are complete.

                // Acceleration vectors
                Double aX = LastVectorSum[bodyNum].X / simBody.Mass;
                Double aY = LastVectorSum[bodyNum].Y / simBody.Mass;
                Double aZ = LastVectorSum[bodyNum].Z / simBody.Mass;

                // Velocity vectors at end of interval given FVs from beginning of interval
                Double eVX = simBody.VX + aX * seconds;
                Double eVY = simBody.VY + aY * seconds;
                Double eVZ = simBody.VZ + aZ * seconds;

                // Location/displacement at end of interval (Position from velocity and acceleration)
                Double newX = simBody.X + simBody.VX * seconds + 5E-1D * aX * secondsSquared;
                Double newY = simBody.Y + simBody.VY * seconds + 5E-1D * aY * secondsSquared;
                Double newZ = simBody.Z + simBody.VZ * seconds + 5E-1D * aZ * secondsSquared;

                // Update body to its new position and velocity vectors
                simBody.SetPosAndVel(seconds, newX, newY, newZ, eVX, eVY, eVZ);
            }

            // Calc FVs at new location (for next iteration)
            CalcForceVectors(); // Between each pair of bodies
            for (int bodyNum = 0; bodyNum < NumBodies; bodyNum++)
                if (!SimBodyList.BodyList[bodyNum].ExcludeFromSim)
                    SumForceVectors(bodyNum, ref LastVectorSum[bodyNum]); // Sum of FVs acting upon this body

#if false
                Vector3d vVec = new(vX, vY, vZ);
                Vector3d vNVec = new(vVec);
                vNVec.Normalize();
                Double vel = vVec.Length;
                String mphStr = "(" + (2236.94D * vel).ToString("#,##0") + " mph)";

                System.Diagnostics.Debug.WriteLine("NextPosition:IterateOnce, "
                    + " " + simBody.Name
                    + " dY Vel " + ((forceVector.Y * seconds) / simBody.Mass).ToString("E")
                    + " vVec (X,Y,Z) (" + vVec.X + "," + vVec.Y + "," + vVec.Z + ")"
                    + " vNVec (X,Y,Z) (" + vNVec.X + "," + vNVec.Y + "," + vNVec.Z + ")"
                    + " forceVector (X,Y,Z) (" + forceVector.X + "," + forceVector.Y + "," + forceVector.Z + ")"
                    + " mphStr " + mphStr
                    );
#endif
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

#if false
                    System.Diagnostics.Debug.WriteLine("NextPosition:CalcForceVectors, "
                        + " " + lBody.Name + " to " + hBody.Name
                        + " ForceVector (X,Y,Z) (" + ForceVectors[i].X + "," + ForceVectors[i].Y + "," + ForceVectors[i].Z + ")"
                        + " d km " + (Math.Sqrt(dSquared) / 1000D).ToString("E")
                        + " newtons " + newtons.ToString()
                        );
#endif

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
