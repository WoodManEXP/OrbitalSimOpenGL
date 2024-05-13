using System;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Holds the mass * mass values for bodies in the system so that is calculated
    /// once instead of over and over again.
    /// Nice memory as well as processing/calculation savings - especially as number
    /// of bodies increases.
    /// </summary>
    internal class MassMass
    {
        #region Properties
        private int[] SumOfIntegers { get; set; } // Lookup table so this is not calculated over and over
        private Double[] MassMassValues { get; set; }
        private int NumBodies { get; set; }
        private SparseArray SparseArray { get; set; }
        #endregion

        public MassMass(SimBodyList simBodyList, SparseArray sparseArray)
        {
            SparseArray = sparseArray;
            NumBodies = simBodyList.BodyList.Count;

            // Construct the MassMass lookup table/array.
            // Could be constructed as a NxN matrix, but the matrix is symmetrical and
            // diagonal values are not needed. So it is made as a 1D array with a
            // function to generate indices into the array. As model size increases
            // this is nice memory/space savings.
            // Number of entries needed is (NumBodies - 1) * NumBodies / 2
            MassMassValues = new Double[SparseArray.NumSlots/*(NumBodies - 1) * NumBodies / 2*/];

            CalcMassMass(simBodyList);
        }
        /// <summary>
        /// Get MassMass value of two bodies in the system
        /// </summary>
        /// <param name="body1">Position of one body in SimBodyList</param>
        /// <param name="body2">Position of another body in SimBodyList</param>
        /// <returns></returns>
        public Double GetMassMass(int body1, int body2)
        {
            return MassMassValues[SparseArray.ValuesIndex(body1, body2)];
        }

        /// <summary>
        /// Fill in MassMass values from current state of SimBodyList
        /// </summary>
        public void CalcMassMass(SimBodyList simBodyList)
        {
            for (int bL = 0; bL < NumBodies; bL++)       // bL - body low number
                for (int bH = 0; bH < NumBodies; bH++)    // bH - body high number
                {
                    // Nothing for diagonal or entries below the diagonal
                    if (bH <= bL)
                        continue;

                    // Where this bL, bH combo lands in the MassMass table/array
                    int i = SparseArray.ValuesIndex(bL, bH);

                    SimBody lSB = simBodyList.BodyList[bL];
                    SimBody hSB = simBodyList.BodyList[bH];

                    Double lBodyMass = lSB.Mass;
                    Double hBodyMass = hSB.Mass;

                    MassMassValues[i] = lBodyMass * hBodyMass;
                }
        }
    }
}
