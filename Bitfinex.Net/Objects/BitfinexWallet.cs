using Bitfinex.Net.Converters;
using Bitfinex.Net.Enum;
using Newtonsoft.Json;

namespace Bitfinex.Net.Objects
{
    public class BitfinexWallet
    {
        /// <summary>
        /// “trading”, “deposit” or “exchange”
        /// </summary>
        [JsonProperty("type"), JsonConverter(typeof(WalletTypeConverter))]
        public WalletType Type { get; set; }

        /// <summary>
        /// Ex: BTC, USD
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        /// <summary>
        /// How much balance of this currency in this wallet
        /// </summary>
        [JsonProperty("amount")]
        public double Amount { get; set; }

        /// <summary>
        /// How much X there is in this wallet that is available to trade
        /// </summary>
        [JsonProperty("available")]
        public double Available { get; set; }

    }
}