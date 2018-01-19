using Newtonsoft.Json;

namespace Bitfinex.Net.Objects.SocketObjets
{
    public class BitfinexSocketErrorMessage : BitfinexSocketMessagesBase
    {
        [JsonProperty("msg")]
        public string ErrorMessage { get; set; }

        [JsonProperty("code")]
        public int ErrorCode { get; set; }
    }
}
