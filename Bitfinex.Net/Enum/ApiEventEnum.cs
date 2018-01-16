namespace Bitfinex.Net.Enum
{
    public enum ApiRequestEventEnum
    {
        Info,
        Ping,
        Subscribe,
        Unsubscribe
    }
    public enum ApiResponseEventEnum
    {
        Pong,
        Subscribed,
        Unsubscribed,
        Error
    }
}