using System.Collections.Generic;

namespace MaxUndercut
{
    /// <summary>
    /// A class allowing comparsion between integer arrays based on a particular index
    /// </summary>
    public class ArrayComparer : IComparer<int[]>
    {
        /// <summary>
        /// The index at which arrays will be compared
        /// </summary>
        private int compareIndex;

        /// <summary>
        /// Parameterized constructor taking the comparison index
        /// </summary>
        /// <param name="compareIndex"></param>
        public ArrayComparer(int compareIndex)
        {
            this.compareIndex = compareIndex;
        }

        /// <summary>
        /// Compares two integer arrays based on their values at a given index
        /// </summary>
        /// <param name="x">The first array to compare</param>
        /// <param name="y">The second array to compare</param>
        /// <returns>The integer result of comparing the values stored at the compareIndex of both arrays</returns>
        int IComparer<int[]>.Compare(int[] x, int[] y)
        {
            return x[this.compareIndex].CompareTo(y[this.compareIndex]);
        }
    }
}
