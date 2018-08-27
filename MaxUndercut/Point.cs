using System;

namespace MaxUndercut
{
    /// <summary>
    /// Stores the relevant information from a candle, namely the date and the price
    /// </summary>
    public class Point : IComparable<Point>
    {
        /// <summary>
        /// The time that the candle began stored as milliseconds since epoch
        /// </summary>
        public long Date { get; set; }

        /// <summary>
        /// The lowest price of the stock in cents during the candle
        /// </summary>
        public int Price { get; set; }

        /// <summary>
        /// Compares two Points based on their date
        /// </summary>
        /// <param name="other">The other point against which to compare</param>
        /// <returns>The integer result of comparing the dates of the two points</returns>
        public int CompareTo(Point other)
        {
            return this.Date.CompareTo(other.Date);
        }
    }
}
