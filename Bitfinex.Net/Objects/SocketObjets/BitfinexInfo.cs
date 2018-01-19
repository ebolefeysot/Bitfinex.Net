using Bitfinex.Net.Enum;
using Newtonsoft.Json;

namespace Bitfinex.Net.Objects.SocketObjets
{
    public class BitfinexInfo : BitfinexSocketMessagesBase
    {
        public int Version { get; set; }
        public int Code { get; set; }
        [JsonProperty("msg")]
        public string Message { get; set; }
    }
    public class BitfinexInfoNew : BitfinexSocketMessagesBase
    {
        [JsonProperty("version")]
        public int Version { get; set; }
    }
    public class BitfinexSubscribedNew : BitfinexSocketMessagesBase
    {
        [JsonProperty("channel")]
        public ChannelEnum Channel { get; set; }

        [JsonProperty("chanId")]
        public int ChannelId { get; set; }
    }
}
