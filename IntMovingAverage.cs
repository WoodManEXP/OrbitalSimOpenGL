using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace OrbitalSimOpenGL
{
    public class IntMovingAverage
    {
        int[] Values;
        int NumSlots, NumInSet, HeadPosn, TailPosn, Total;
        public int MovingAverage { get; set; }

        /// <summary>
        /// MoningAverage calculator (for int)
        /// </summary>
        /// <param name="numSlots">Max num values to retain for moving average</param>
        public IntMovingAverage(int numSlots)
        {
            Values = new int[NumSlots = numSlots];
            Reset();
        }
        /// <summary>
        /// Add another value to moving average claculation
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Current moving average, after new value added.</returns>
        public int AnotherValue(int value)
        {
            // Set is full. Remove value at TailPosn
            if (NumInSet >= NumSlots)
            {
                Total -= Values[TailPosn];  // Remove this value from the Total
                if (++TailPosn >= NumSlots) // Advance the tail
                    TailPosn = 0;
            }
            else 
                NumInSet++;

            Total += value;
            Values[HeadPosn] = value;

            if (++HeadPosn >= NumSlots)     // Advance the head
                HeadPosn = 0;

            return MovingAverage = Total / NumInSet;
        }

        // Back to empty
        public void Reset()
        {
            NumInSet = HeadPosn = TailPosn = 0;
            Total = 0;
        }
    }
}
