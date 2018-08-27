namespace MaxUndercut
{
    /// <summary>
    /// Organizes the price history information needed for maximum undercut calculations
    /// </summary>
    public class DataSet
    {
        public Point[] Points { get; set; }

        public int[] WeekendIndexes { get; set; }

        public int AveragePrice { get; set; }
    }
}
