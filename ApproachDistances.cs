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

        private Double[] CApproach { get; set; } // Closest approach, km
        private Double[] FApproach { get; set; } // Furthest approach, km
        private int NumBodies { get; set; }
        private SparseArray SumOfIntegers { get; set; }

        public ApproachDistances(int numBodies, SparseArray sumOfIntegers)
        {
            NumBodies = numBodies;
            SumOfIntegers = sumOfIntegers;

            // Construct the Vectors lookup table/array.
            // Could be constructed as a NxN matrix, but the matrix is symmetrical and
            // diagonal values are not needed. So it is made as a 1D array with a
            // function to generate indices into the array. As model size increases
            // this is nice memory/space savings.
            // Number of entries needed is (NumBodies - 1) * NumBodies / 2
            CApproach = new Double[(NumBodies - 1) * NumBodies / 2];
            FApproach = new Double[(NumBodies - 1) * NumBodies / 2];
            //Reset();

        }

        /// <summary>
        /// Record closest and furtherest approach values between two bodies
        /// </summary>
        /// <param name="lBody"></param>
        /// <param name="hBody"></param>
        /// <param name="approachDist"></param>
        public void SetApproach(int lBody, int hBody, Double approachDist)
        {
            if (lBody == hBody)
                return;
            if (lBody > hBody)
                (lBody, hBody) = (hBody, lBody); // Tuple swap
            /*
                _ = hBody ^= lBody ^= hBody;

            // XOR swap 
               int a = 4, b = 6;
                a ^= b ^= a ^= b;
            */


        }
#if false
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
                    int i = ValuesIndex(bL, bH);

                    // Each force vector's length is the Newtons of force the pair of bodies exert on one another.
                    ForceVectors[i] *= newtons;
                }
            }
        }
#endif
    }
}
