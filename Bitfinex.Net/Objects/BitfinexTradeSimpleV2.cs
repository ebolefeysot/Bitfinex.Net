using System;
using Bitfinex.Net.Converters;
using Bitfinex.Net.Enum;
using Newtonsoft.Json;

namespace Bitfinex.Net.Objects
{
    [JsonConverter(typeof(BitfinexResultConverter))]
    public class BitfinexTradeSimpleV2
    {
        [BitfinexProperty(0)]
        public long Id { get; set; }
        [BitfinexProperty(1), JsonConverter(typeof(TimestampConverter))]
        public DateTime Timestamp { get; set; }
        [BitfinexProperty(2)]
        public double Amount { get; set; }
        [BitfinexProperty(3)]
        public double Price { get; set; }

        public TypeEnum Type => Amount < 0 ? TypeEnum.Sell : TypeEnum.Buy;
    }

    public class BitfinexTradeSimple
    {
        //{"timestamp":1517161095,"tid":180372210,"price":"11750.0","amount":"0.01","exchange":"bitfinex","type":"buy"}
        [JsonProperty("timestamp"), JsonConverter(typeof(TimestampSecondsConverter))]
        public DateTime Timestamp { get; set; }
        [JsonProperty("tid")]
        public long Id { get; set; }
        [JsonProperty("price")]
        public double Price { get; set; }
        [JsonProperty("amount")]
        public double Amount { get; set; }
        [JsonProperty("type")]
        public TypeEnum Type { get; set; }
    }
}
