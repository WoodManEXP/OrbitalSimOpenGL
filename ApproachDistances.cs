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
#if false
        internal struct ApproachElement
        {
            public Double CDist { get; set; }       // Closest approach, km-squared
            public Double CSeconds { get; set; }    // Timestamp for closest approach
            public Double FDist { get; set; }       // Furthest approach, km-squared
            public Double FSeconds { get; set; }    // Timestamp for furthest approach
        }
        internal ApproachElement[] ApproachElements { get; set; }
#endif
        internal ApproachElements ApproachElements { get; set; }
        internal int NumBodies { get; set; }
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
            ApproachElements = new ApproachElements(SparseArray.NumSlots);

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

            /*
                _ = hBody ^= lBody ^= hBody;

            // XOR swap 
               int a = 4, b = 6;
                a ^= b ^= a ^= b;
            */
            if (lBody > hBody)
                (lBody, hBody) = (hBody, lBody); // Tuple swap. Could have used XOR swap. But this is the C# way

            int i = SparseArray.ValuesIndex(lBody, hBody);
            
            // Closest approach ?
            if (distanceSquared < ApproachElements.Elements[i].CDist)
            {
                ApproachElements.Elements[i].CDist = distanceSquared;
                ApproachElements.Elements[i].CSeconds = seconds;
            }

            // Furthest approach ?
            if (distanceSquared > ApproachElements.Elements[i].FDist)
            {
                ApproachElements.Elements[i].FDist = distanceSquared;
                ApproachElements.Elements[i].FSeconds = seconds;
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
                    ApproachElements.Elements[i].CDist = Double.MaxValue;
                    ApproachElements.Elements[i].FDist = Double.MinValue;
                    ApproachElements.Elements[i].CSeconds = ApproachElements.Elements[i].FSeconds = 0D;
                }
            }
        }
    }
}
