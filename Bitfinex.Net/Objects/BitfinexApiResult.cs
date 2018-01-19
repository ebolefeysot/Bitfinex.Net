using Bitfinex.Net.Enum;
using Bitfinex.Net.Objects.SocketObjets;
using Newtonsoft.Json;

namespace Bitfinex.Net.Objects
{
    /// <summary>
    /// The result of an Api call
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    public class BitfinexApiResult<T>
    {
        /// <summary>
        /// Whether the Api call was successful
        /// </summary>
        [JsonProperty("success")]
        public bool Success { get; internal set; }

        /// <summary>
        /// The result of the Api call
        /// </summary>
        [JsonProperty("result")]
        public T Result { get; internal set; }

        /// <summary>
        /// The message if the call wasn't successful
        /// </summary>
        [JsonProperty("message")]
        public BitfinexError Error { get; internal set; }
    }

    public class BitfinexApiSubscriptionResponse : BitfinexSocketMessagesBase
    {
        public BitfinexApiSubscriptionResponse(BitfinexSocketClient bitfinexSocketClient)
        {
            client = bitfinexSocketClient;
        }

        /// <summary>
        /// Whether the Api call was successful
        /// </summary>
        [JsonProperty("channel")]
        public ChannelEnum Channel { get; internal set; }

        /// <summary>
        /// The result of the Api call
        /// </summary>
        [JsonProperty("chanId")]
        public int ChannelId { get; internal set; }

        /// <summary>
        /// Unsubscribe to this channel
        /// </summary>
        /// <returns></returns>
        public BitfinexSocketMessagesBase Unsubscribe()
        {
            return client.Unsubscribe(ChannelId);
        }

        private BitfinexSocketClient client;
    }

    public class BitfinexApiFailureResponse : BitfinexSocketMessagesBase
    {
        /// <summary>
        /// Whether the Api call was successful
        /// </summary>
        [JsonProperty("msg")]
        public string Message { get; internal set; }

        /// <summary>
        /// The result of the Api call
        /// </summary>
        [JsonProperty("code")]
        public int Code { get; internal set; }
    }
}
