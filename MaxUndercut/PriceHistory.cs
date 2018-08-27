using System.Runtime.Serialization;

namespace MaxUndercut
{
    /// <summary>
    /// Organizes the information stored in the JSON returned by the TDAmeritrade PriceHistory API
    /// </summary>
    [DataContract]
    public class PriceHistory
    {
        /// <summary>
        /// An array of candles organizing the price of the stock across the requested period
        /// </summary>
        [DataMember]
        internal Candle[] candles;

        /// <summary>
        /// True if no price history was found for the stock
        /// </summary>
        [DataMember]
        internal bool empty;

        /// <summary>
        /// The symbol of the stock
        /// </summary>
        [DataMember]
        internal string symbol;
    }
}
