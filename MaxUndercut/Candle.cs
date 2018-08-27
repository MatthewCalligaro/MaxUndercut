using System.Runtime.Serialization;

namespace MaxUndercut
{
    /// <summary>
    /// Encodes the information associated with a candle in the TDAmeritrade PriceHistory API
    /// </summary>
    [DataContract]
    public class Candle
    {
        /// <summary>
        /// The price of the stock at the end of the candle
        /// </summary>
        [DataMember]
        internal double close;

        /// <summary>
        /// The time that the candle began stored as milliseconds since epoch
        /// </summary>
        [DataMember]
        internal long datetime;

        /// <summary>
        /// The highest price of the stock during the candle
        /// </summary>
        [DataMember]
        internal double high;

        /// <summary>
        /// The lowest price of the stock during the candle
        /// </summary>
        [DataMember]
        internal double low;

        /// <summary>
        /// The price of the stock at the beginning of the candle
        /// </summary>
        [DataMember]
        internal double open;

        /// <summary>
        /// The shares of the stock bought/sold during the candle
        /// </summary>
        [DataMember]
        internal int volume;
    }
}
