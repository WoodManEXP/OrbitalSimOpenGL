using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Lookup table so this is not calculated over and over
    /// </summary>
    internal class SparseArray
    {
        #region Properties
        private readonly int NumIntegers;
        private readonly int[] Sum;

        public int this[int index]
        {
            get => Sum[index]; // Return SumOfIntegers value
        }
        #endregion

        public SparseArray(int numIntegers)
        {
            NumIntegers = numIntegers;

            // Construct sum of integers table/array
            Sum = new int[NumIntegers];
            Sum[0] = 0;
            for (int i = 1; i < NumIntegers; i++)
                Sum[i] = i + Sum[i - 1];
        }

        /// <summary>
        /// Generate index into Vectors array
        /// </summary>
        /// <param name="body0">One body number - position in SimBodyList/param>
        /// <param name="body1">Other body number - position in SimBodyList</param>
        /// <returns></returns>
        public int ValuesIndex(int body0, int body1)
        {
            var lBL = Math.Min(body0, body1);
            var lBh = Math.Max(body0, body1);
            return (lBL * NumIntegers) - Sum[lBL] + lBh - lBL - 1;
        }
    }
}
