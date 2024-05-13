using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Collects Closest and Furtherest approach values
    /// </summary>
    internal class ApproachDistances
    {
        private struct ApproachElement
        {
            public Double CDist { get; set; }       // Closest approach, km-squared
            public Double CSeconds { get; set; }    // Timestamp for closest approach
            public Double FDist { get; set; }       // Furthest approach, km-squared
            public Double FSeconds { get; set; }    // Timestamp for closest approach
        }

        private ApproachElement[] Approach { get; set; }
        private int NumBodies { get; set; }
        private SparseArray SparseArray { get; set; }

        public ApproachDistances(int numBodies, SparseArray sparseArray)
        {
            NumBodies = numBodies;
            SparseArray = sparseArray;

            // Construct the Vectors lookup table/array.
            // Could be constructed as a NxN matrix, but the matrix is symmetrical and
            // diagonal values are not needed. So it is made as a 1D array with a
            // function to generate indices into the array. As model size increases
            // this is nice memory/space savings.
            // Number of entries needed is (NumBodies - 1) * NumBodies / 2
            Approach = new ApproachElement[SparseArray.NumSlots];
            Reset();

        }

        /// <summary>
        /// Record closest and furthest approach values between two bodies
        /// </summary>
        /// <param name="lBody"></param>
        /// <param name="hBody"></param>
        /// <param name="distanceSquared"></param>
        public void SetApproach(int lBody, int hBody, Double distanceSquared, Double seconds)
        {
            if (lBody == hBody)
                return;
            if (lBody > hBody)
                (lBody, hBody) = (hBody, lBody); // Tuple swap. Could have used XOR swap. But this is the C# way
            /*
                _ = hBody ^= lBody ^= hBody;

            // XOR swap 
               int a = 4, b = 6;
                a ^= b ^= a ^= b;
            */
            int i = SparseArray.ValuesIndex(lBody, hBody);
            
            // Closest approach ?
            if (distanceSquared < Approach[i].CDist)
            {
                Approach[i].CDist = distanceSquared;
                Approach[i].CSeconds = seconds;
            }

            // Furthest approach ?
            if (distanceSquared > Approach[i].FDist)
            {
                Approach[i].FDist = distanceSquared;
                Approach[i].FSeconds = seconds;
            }
        }

        /// <summary>
        /// Reset to initial state
        /// </summary>
        public void Reset()
        {
            for (int bL = 0; bL < NumBodies; bL++)       // bL - body low number
            {
                for (int bH = 0; bH < NumBodies; bH++)    // bH - body high number
                {
                    // Nothing for diagonal or below the diagonal entries 
                    if (bH <= bL)
                        continue;

                    // Index of this bL, bH combo into the Vectors table/array.
                    // Same algorithm as used in MassMass.
                    int i = SparseArray.ValuesIndex(bL, bH);

                    // Set initial values
                    Approach[i].CDist = Double.MaxValue;
                    Approach[i].FDist = Double.MinValue;
                    Approach[i].CSeconds = Approach[i].FSeconds = 0D;
                }
            }
        }
    }
}
