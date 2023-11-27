﻿using System;

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
        #endregion

        public MassMass(SimBodyList simBodyList)
        {
            NumBodies = simBodyList.BodyList.Count;

            // Construct sum of integers table/array
            SumOfIntegers = new int[NumBodies];
            SumOfIntegers[0] = 0;
            for (int i = 1; i < NumBodies; i++)
                SumOfIntegers[i] = i + SumOfIntegers[i - 1];

            // Construct the MassMass lookup table/array.
            // Could be constructed as a NxN matrix, but the matrix is symmetrical and
            // diagonal values are not needed. So it is made as a 1D array with a
            // function to generate indices into the array. As model size increases
            // this is nice memory/space savings.
            // Number of entries needed is (NumBodies - 1) * NumBodies / 2
            MassMassValues = new Double[(NumBodies - 1) * NumBodies / 2];

            for(int bL = 0; bL < NumBodies; bL++)       // bL - body low number
                for (int bH=0; bH < NumBodies; bH++)    // bH - body high number
                {
                    // Nothing for diagonal or entries below the diagonal
                    if (bH <= bL)
                        continue;

                    // Where this bL, bH combo lands in the MassMass table/array
                    int i = ValuesIndex(bL, bH);

                    Double lBodyMass = simBodyList.BodyList[bL].Mass;
                    Double hBodyMass = simBodyList.BodyList[bH].Mass;

                    MassMassValues[i] = lBodyMass * hBodyMass;
                }
        }
        /// <summary>
        /// Get MassMass value of two bodies in the system
        /// </summary>
        /// <param name="body1">Position of one body in SimBodyList</param>
        /// <param name="body2">Position of another body in SimBodyList</param>
        /// <returns></returns>
        public Double GetMassMass(int body1, int body2)
        {
            return MassMassValues[ValuesIndex(Math.Min(body1, body2), Math.Max(body1, body2))];
        }
        /// <summary>
        /// Generate index into MassMassValues array
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
