using Bitfinex.Net.Enum;
using Bitfinex.Net.Objects.SocketObjets;
using Newtonsoft.Json;

namespace Bitfinex.Net.Objects
{
    internal class BitfinexApiUnsubscriptionResponse : BitfinexSocketMessagesBase
    {
        [JsonProperty("status")]
        public ChannelEnum Status { get; set; }

        [JsonProperty("chanId")]
        public int ChannelId { get; set; }
    }
}