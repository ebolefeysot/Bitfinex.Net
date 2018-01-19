using Bitfinex.Net.Enum;
using Newtonsoft.Json;

namespace Bitfinex.Net.Objects.SocketObjets
{
    public class BitfinexSocketUnsubscribeMessage : BitfinexSocketMessagesBase
    {
        /// <summary>
        /// Unsubscription numeric channel identifier. Provided at the subscription.
        /// </summary>
        [JsonProperty("chanId")]
        public int ChannelId { get; set; }
    }

    public class BitfinexSocketUnsubscribedResponse : BitfinexSocketMessagesBase
    {
        /// <summary>
        /// Unsubscription numeric channel identifier. Provided at the subscription.
        /// </summary>
        [JsonProperty("chanId")]
        public int ChannelId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}