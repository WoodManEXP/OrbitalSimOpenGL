using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
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

        internal SimModel SimModel { get; set; }
        private int NumBodies { get; set; }
        private SimBodyList SimBodyList { get; set; }
        internal MassMass? MassMass { get; set; }
        internal Barycenter? Barycenter { get; set; }

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
        private CollisionDetector CollisionDetector { get; set; }
        #endregion

        /// <summary>
        /// Repositioning iterations
        /// </summary>
        /// <param name="simBodyList"></param>
        /// <param name="massMass"></param>
        /// <param name="initialGravConstantSetting"></param>
        /// <param name="collisionDetector"></param>
        public NextPosition(SimModel simModel, Double initialGravConstantSetting)
        {
            SimModel = simModel;
            SimBodyList = SimModel.SimBodyList;
            NumBodies = SimBodyList.BodyList.Count;
            CollisionDetector = SimModel.CollisionDetector;
            Barycenter = SimModel.Barycenter;

            UseReg_G = initialGravConstantSetting;

            MassMass = SimModel.MassMass;
            SparseArray = SimModel.SparseArray;

            // Construct the Vectors lookup table/array.
            // Could be constructed as a NxN matrix, but the matrix is symmetrical and
            // diagonal values are not needed. So it is made as a 1D array with a
            // function to generate indices into the array. As model size increases
            // this is nice memory/space savings.
            // Number of entries needed is (NumBodies - 1) * NumBodies / 2
            ForceVectors = new Vector3d[SparseArray.NumSlots];

            // Prior to beginning iterations: Calc & sum FVs, save in LastVectorSum 
            CalcForceVectors(); // Between each pair of bodies
            SumForceVectors(false);  // Acting upon bodies
        }

        /// <summary>
        /// Reposition bodies for a single iteration
        /// </summary>
        /// <param name="seconds">Target number of seconds to elapse for the iteration.</param>
        /// <remarks>
        /// Degenerate cases are when the FV during an interval flips 180 degrees, or nearly.
        /// Meaning collision detection is disabled and bodies pass thorugh one another.
        /// </remarks>
        public Double IterateOnce(Double seconds)
        {
            IterationNumber++;

            ProcessInterval(seconds);

            // Look for and make adjustments for the degenerate case(s)
            seconds = DegenCase(seconds);

            return seconds;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="seconds"></param>
        private void ProcessInterval(Double seconds)
        {
            // Process the interval
            SimBody simBody;
            int bodyNum;

            // If not detecting collisions then degen case may emerge. In which case
            // previous interval's values are needed.
            bool savePrevious = !CollisionDetector.DetectCollisions;

            for (bodyNum = 0; bodyNum < NumBodies; bodyNum++)
            {
                simBody = SimBodyList.BodyList[bodyNum];

                if (simBody.ExcludeFromSim)
                    continue; // No need if body has been excluded

                // New velocity vectors
                Double eVX = simBody.VX + ((simBody.CurrFV.X * seconds) / simBody.Mass);
                Double eVY = simBody.VY + ((simBody.CurrFV.Y * seconds) / simBody.Mass);
                Double eVZ = simBody.VZ + ((simBody.CurrFV.Z * seconds) / simBody.Mass);

                // If new vel is over 1/10th c (Does this ever happen ?)
                Vector3d vVec = new(eVX, eVY, eVZ);
                if (Util.C_OneTenthSquaredhKMS < vVec.LengthSquared)
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

                simBody.SetPosAndVel(savePrevious, seconds, newX, newY, newZ, eVX, eVY, eVZ); // Most of time these setings will stick
            }

            // Calculate barycenter
            Barycenter?.Calc();

            // Calc FVs for next pass
            CalcForceVectors();
            SumForceVectors(savePrevious);
        }

        /// <summary>
        /// Restore bodies position and velocity vectors to beginning of interval
        /// </summary>
        private void ResetInterval()
        {

            // Reset the interval
            SimBody simBody;
            int bodyNum;

            for (bodyNum = 0; bodyNum < NumBodies; bodyNum++)
            {
                simBody = SimBodyList.BodyList[bodyNum];

                if (simBody.ExcludeFromSim)
                    continue; // No need if body has been excluded

                // Restore position and velocity vectors
                simBody.RestoreToPrev();
            }
        }

        struct CrossTime
        {
            #region Properties
            public Double Seconds { get; set; } //Seconds into the interval to the FV flip
            #endregion

            /// <summary>
            /// One of these is instantiated each time a FV flip is detected.
            /// </summary>
            /// <param name="simBody">Body for which FV vector flip was detected</param>
            /// <remarks>
            /// With the FV 180 degree flip this knows the body centers were on a direct collision course...
            /// </remarks>
            public CrossTime(SimBody simBody, Barycenter barycenter)
            {

                // Calculate the seconds into the interval of the FV flip
                Seconds = 0D;

                // Known: Beginning and end of interval position and velocity vectors of all bodies in the system
                // Flip seconds into the interval are calculated using beginning of interval values
                // (those are the ones used to advance bodies through the interval)
                // Cross time is calculated on
                // - simBody's beginning of interval position and velocity vector through the interval
                // - Barycenter location and velocity as defined by all the other bodies in the system (excluding simBody)
                // https://orbital-mechanics.space/the-n-body-problem/motion-of-the-barycenter.html

                // Location and velocity of simBody
                // (Supposedly allocation of a struct via "new" in this context is stack rather than heap space. So this is
                // as efficiant as it can be).
                Vector3d simBodyLoc = new(simBody.X, simBody.Y, simBody.Z);
                Double simBodyVel = new Vector3d(simBody.VX, simBody.VY, simBody.VZ).Length;

                // Location and velocity of simBody'less barycenter
                Vector3d barycenterLoc = (barycenter.Numerator - (simBody.Mass * simBodyLoc)) / (barycenter.SystemMass - simBody.Mass);
                Double barycenterVel = barycenter.VelocityLess(simBody);

                Seconds = (simBodyLoc - barycenterLoc).Length / (simBodyVel + barycenterVel);
            }
        }
        private List<CrossTime> CrossTimesList = new();

        /// <summary>
        /// Handle degenerate case of bodies passing through one another
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        private Double DegenCase(Double seconds)
        {
            // If collisions are being detected no need to process the degenerate case(s)
            if (CollisionDetector.DetectCollisions)
                return seconds;

            CrossTimesList.Clear();

            // Did any of the force vectors flip?
            // Collect in the list refs to each of the bodies for which FVs flipped.
            for (int bodyNum = 0; bodyNum < NumBodies; bodyNum++)
            {
                SimBody simBody = SimBodyList.BodyList[bodyNum];

                if (simBody.ExcludeFromSim)
                    continue;

                if (VectorsAreOpposed(simBody.CurrFV, simBody.PrevFV))
                {
                    // Here it is known the FV for simBody has encountered an ~180 flip
                    // (two bodies have passed through one another (collision detection is off))
                    // Although unlikely, this could happen for multiple bodies during an iteration.
                    // So a list of the interval's 180 degree crossings is collected and the earliest crossing is processed.
                    // Not totally fool-proof, but given bodies tend to be widely spaced and intervals short
                    // most cases will be processed correctly. If there were many bodies really close to one another
                    // in a tight group this will likely generate poor results.
                    CrossTimesList.Add(new(simBody, Barycenter));
                }
#if true
                SimBody simBody0 = SimBodyList.BodyList[0];
                SimBody simBody1 = SimBodyList.BodyList[1];
                Vector3d sv0, sv1;
                Double len;
                sv0.X = simBody0.X; sv0.Y = simBody0.Y; sv0.Z = simBody0.Z;
                sv1.X = simBody1.X; sv1.Y = simBody1.Y; sv1.Z = simBody1.Z;
                len = (sv0 - sv1).Length;
                if (0 == bodyNum)
                    System.Diagnostics.Debug.WriteLine("NextPosition:DegenCase bodyNum 0"
                        + " Dist " + len.ToString("0.000E00")
                        + " X " + simBody.X.ToString("0.000E00")
                        + " PrevX " + simBody.PrevLoc.X.ToString("0.000E00")
                        + " VelVec " + simBody.VX.ToString("0.000E00")
                        + " PrevVel " + simBody.PrevVel.X.ToString("0.000E00")
                        + " CurrFV " + simBody.CurrFV.X.ToString("0.000E00")
                        + " PrevFV " + simBody.PrevFV.X.ToString("0.000E00")
                     );
#endif
            }

            // Pull out min crosstimes seconds as return value
            if (CrossTimesList.Count > 0)
            {
                seconds = Double.MaxValue;
                foreach (CrossTime crossTime in CrossTimesList)
                    seconds = Math.Min(seconds, crossTime.Seconds);
                // Double it to get to place on other side of flip where FVs
                // should be about what they were upon entering the interval.
                seconds *= 2D;

                ResetInterval();            // Back to beginning of interval

#if true
                SimBody simBody0 = SimBodyList.BodyList[0];
                SimBody simBody1 = SimBodyList.BodyList[1];
                Vector3d sv0, sv1;
                Double len;
                sv0.X = simBody0.X; sv0.Y = simBody0.Y; sv0.Z = simBody0.Z;
                sv1.X = simBody1.X; sv1.Y = simBody1.Y; sv1.Z = simBody1.Z;
                len = (sv0 - sv1).Length;
                System.Diagnostics.Debug.WriteLine("NextPosition:DegenCase bodyNum 0 after ResetInterval"
                    + " Dist " + len.ToString("0.000E00")
                    + " seconds " + seconds.ToString("0.000E00")
                    + " X " + simBody0.X.ToString("0.000E00")
                    + " PrevX " + simBody0.PrevLoc.X.ToString("0.000E00")
                    + " VelVec " + simBody0.VX.ToString("0.000E00")
                    + " PrevVel " + simBody0.PrevVel.X.ToString("0.000E00")
                    + " CurrFV " + simBody0.CurrFV.X.ToString("0.000E00")
                    + " PrevFV " + simBody0.PrevFV.X.ToString("0.000E00")
                    );
#endif

                ProcessInterval(seconds);   // Process it over again wiht the new value for seconds

#if true
                sv0.X = simBody0.X; sv0.Y = simBody0.Y; sv0.Z = simBody0.Z;
                sv1.X = simBody1.X; sv1.Y = simBody1.Y; sv1.Z = simBody1.Z;
                len = (sv0 - sv1).Length;
                System.Diagnostics.Debug.WriteLine("NextPosition:DegenCase bodyNum 0 after reprocess interval"
                    + " Dist " + len.ToString("0.000E00")
                    + " seconds " + seconds.ToString("0.000E00")
                    + " X " + simBody0.X.ToString("0.000E00")
                    + " PrevX " + simBody0.PrevLoc.X.ToString("0.000E00")
                    + " VelVec " + simBody0.VX.ToString("0.000E00")
                    + " PrevVel " + simBody0.PrevVel.X.ToString("0.000E00")
                    + " CurrFV " + simBody0.CurrFV.X.ToString("0.000E00")
                    + " PrevFV " + simBody0.PrevFV.X.ToString("0.000E00")
                    );
#endif
            }

            return seconds;
        }

        /// <summary>
        /// Sun Force Vectors acting on each body
        /// </summary>
        /// <param name="bodyNum"></param>
        /// <param name="forceVector"></param>
        /// <remarks>
        /// FVs represent Newtons of force in each dimension.
        /// bodyNum should not refer to an excluded body.
        /// </remarks>
        private void SumForceVectors(bool savePrevious)
        {
            for (int bodyNum = 0; bodyNum < NumBodies; bodyNum++)
            {
                SimBody simBody = SimBodyList.BodyList[bodyNum];

                if (simBody.ExcludeFromSim)
                    continue;

                if (savePrevious)
                    simBody.PrevFV = simBody.CurrFV;

                simBody.CurrFV -= simBody.CurrFV; // To 0

                //forceVector -= forceVector; // To 0

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
                            simBody.CurrFV += ForceVectors[index];
                        else
                            simBody.CurrFV -= ForceVectors[index];
                    }
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
        /// Determine if two double precision 3D vectors are opposite
        /// </summary>
        /// <param name="v">One vector</param>
        /// <param name="w">Another vector</param>
        /// <param name="tolerance">Variation tollerated among the three calculated b values</param>
        /// <returns>true or false</returns>
        /// <remarks>
        /// Two vectors are opposite if v = -bw, with b a scalar multiple.
        /// As being not opposite is far and away the most common case, ruleout has highest priority.
        /// Most expensive case is 3 double precision divisions, several subtractions
        /// </remarks>
        bool VectorsAreOpposed(Vector3d v, Vector3d w, Double tolerance = 1e-6)
        {
            Double bX, bY, bZ;
            bool bXZero, bYZero, bZZero;
#if false
            System.Diagnostics.Debug.WriteLine("NextPosition:VectorsAreOpposed "
                + "v " + v
                + ", w" + w
             );

            if ((v.X < 0D && w.X > 0D) || (v.X > 0D && w.X < 0D))
                System.Diagnostics.Debugger.Break();
#endif

            if (v.X == 0D)
                bX = 0D;
            else if (w.X == 0D)
                bX = 0D;
            else // Neither is 0D
                bX = v.X / w.X;

            if (v.Y == 0D)
                bY = 0D;
            else if (w.Y == 0D)
                bY = 0D;
            else // Neither is 0D
                bY = v.Y / w.Y;

            if (v.Z == 0D)
                bZ = 0D;
            else if (w.Z == 0D)
                bZ = 0D;
            else // Neither is 0D
                bZ = v.Z / w.Z;

            // Most common case, quick reject.
            if (bX > 0D || bY > 0D || bZ > 0D)
                return false;

            // Less common cases. Any nonzero values here are negative

            bXZero = bX == 0D;
            bYZero = bY == 0D;
            bZZero = bZ == 0D;

            // All three 0
            if (bXZero && bYZero && bZZero)
                return false;

            // Two are 0D
            if (bXZero && bYZero)
                return (bZ < 0D);
            if (bXZero && bZZero)
                return (bY < 0D);
            if (bYZero && bZZero)
                return (bX < 0D);

            // Two < 0D, one 0D
            if (bXZero)
                return (tolerance > Math.Abs(bY - bZ));
            if (bYZero)
                return (tolerance > Math.Abs(bX - bZ));
            if (bZZero)
                return (tolerance > Math.Abs(bX - bY));

            // All are < 0D
            // See if they are "equal" within tollerance
            Double max = Math.Max(Math.Max(bX, bY), bZ);
            Double min = Math.Min(Math.Min(bX, bY), bZ);
            return (tolerance > max - min) ? true : false;
        }
    }
}
