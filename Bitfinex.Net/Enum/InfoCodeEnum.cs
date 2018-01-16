namespace Bitfinex.Net.Enum
{
    public enum InfoCodeEnum
    {
        /// <summary>
        /// Stop/Restart Websocket Server(please try to reconnect)
        /// </summary>
        Restart = 20051,

        /// <summary>
        /// Refreshing data from the Trading Engine.Please pause any activity and resume after receiving the info message 20061 (it should take 10 seconds at most).
        /// </summary>
        RefreshingData = 20060,

        /// <summary>
        /// Done Refreshing data from the Trading Engine.You can resume normal activity.It is advised to unsubscribe/subscribe again all channels.
        /// </summary>
        DoneRefreshing = 20061,
    }
}
