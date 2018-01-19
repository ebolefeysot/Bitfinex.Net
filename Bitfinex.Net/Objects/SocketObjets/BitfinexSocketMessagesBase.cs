using Bitfinex.Net.Enum;
using Newtonsoft.Json;

namespace Bitfinex.Net.Objects.SocketObjets
{
    /// <summary>
    /// Base class of all socket messages (request and response)
    /// </summary>
    public class BitfinexSocketMessagesBase
    {
        [JsonProperty("event")]
        public string Event { get; set; }

        public ApiEventEnum EventType => (ApiEventEnum)System.Enum.Parse(typeof(ApiEventEnum), Event, true);
    }
}