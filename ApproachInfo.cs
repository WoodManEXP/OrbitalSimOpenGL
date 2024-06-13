using OpenTK.Mathematics;
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
    internal class ApproachInfo
    {
        internal ApproachElements ApproachElements { get; set; }
        internal int NumBodies { get; set; }
        private SparseArray SparseArray { get; set; }

        public ApproachInfo(int numBodies, SparseArray sparseArray)
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
        /// <param name="bL">Lower body number</param>
        /// <param name="bH">Digher body number</param>
        /// <param name="distanceSquared"></param>
        public void SetApproachInfo(int bL, SimBody lBody, int bH, SimBody hBody, Double distanceSquared, Double seconds)
        {
            if (bL == bH)
                return; // JIC

            /*
                _ = hBody ^= lBody ^= hBody;

            // XOR swap 
               int a = 4, b = 6;
                a ^= b ^= a ^= b;
            */

            // This is a JIC check.
            bool swapped = false;
            if (bL > bH)
            {
                swapped = true;
                (bL, bH) = (bH, bL); // Tuple swap. Could have used XOR swap. But this is the C# way
                (lBody, hBody) = (hBody, lBody);
            }

            int i = SparseArray.ValuesIndex(bL, bH);
            
            // Closest approach ?
            if (distanceSquared < ApproachElements.Elements[i].CDist)
            {
                ApproachElements.Elements[i].CDist = distanceSquared;
                ApproachElements.Elements[i].CSeconds = seconds;

                // Relative velocity at this closest approach
                ApproachElements.Elements[i].CVX = swapped ? hBody.VX - lBody.VX : lBody.VX - hBody.VX;
                ApproachElements.Elements[i].CVY = swapped ? hBody.VY - lBody.VY : lBody.VY - hBody.VY;
                ApproachElements.Elements[i].CVZ = swapped ? hBody.VZ - lBody.VZ : lBody.VZ - hBody.VZ;
            }

            // Furthest approach ?
            if (distanceSquared > ApproachElements.Elements[i].FDist)
            {
                ApproachElements.Elements[i].FDist = distanceSquared;
                ApproachElements.Elements[i].FSeconds = seconds;

                // Relative velocity at this closest approach
                ApproachElements.Elements[i].FVX = swapped ? hBody.VX - lBody.VX : lBody.VX - hBody.VX;
                ApproachElements.Elements[i].FVY = swapped ? hBody.VY - lBody.VY : lBody.VY - hBody.VY;
                ApproachElements.Elements[i].FVZ = swapped ? hBody.VZ - lBody.VZ : lBody.VZ - hBody.VZ;
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
