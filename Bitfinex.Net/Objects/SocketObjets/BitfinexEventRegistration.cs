using System;
using System.Collections.Generic;
using System.Threading;
using Bitfinex.Net.Enum;

namespace Bitfinex.Net.Objects.SocketObjets
{
    public class BitfinexEventRegistration
    {
        /// <summary>
        /// Internal id
        /// </summary>
        public long Id { get; set; }

        public List<string> EventTypes { get; set; }

        /// <summary>
        /// Name of the channel subscribed.
        /// </summary>
        public ChannelEnum ChannelName { get; set; }

        /// <summary>
        /// Channel reference id returned by bitfinex. Used to unsubscribe.
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// Flag used to signal a socket request get a response
        /// </summary>
        public ManualResetEvent ResponseEvent { get; } = new ManualResetEvent(false);

        private BitfinexError error;

        public BitfinexError Error
        {
            get => error;
            set
            {
                error = value;
                //Signal the confirmation
                ResponseEvent.Set();
                if (error != null)
                {
                    Status = EventStatusEnum.Failure;
                }
                else
                {
                    Status = EventStatusEnum.Unknown;
                }
            }
        }

        public EventStatusEnum Status { get; set; }

        /// <summary>
        /// Return true if the event get a response.
        /// </summary>
        public bool Confirmed => ResponseEvent.WaitOne(0, false);
    }

    public enum EventStatusEnum
    {
        Pending,
        Failure,
        Subscribed,
        Unknown
    }

    public class BitfinexWalletSnapshotEventRegistration : BitfinexEventRegistration
    {
        public Action<BitfinexWalletV2[]> Handler { get; set; }
    }

    public class BitfinexOrderSnapshotEventRegistration : BitfinexEventRegistration
    {
        public Action<BitfinexOrder[]> Handler { get; set; }
    }

    public class BitfinexPositionsSnapshotEventRegistration : BitfinexEventRegistration
    {
        public Action<BitfinexPosition[]> Handler { get; set; }
    }

    public class BitfinexFundingOffersSnapshotEventRegistration : BitfinexEventRegistration
    {
        public Action<BitfinexFundingOffer[]> Handler { get; set; }
    }

    public class BitfinexFundingCreditsSnapshotEventRegistration : BitfinexEventRegistration
    {
        public Action<BitfinexFundingCredit[]> Handler { get; set; }
    }

    public class BitfinexFundingLoansSnapshotEventRegistration : BitfinexEventRegistration
    {
        public Action<BitfinexFundingLoan[]> Handler { get; set; }
    }



    public class BitfinexTradingPairTickerEventRegistration : BitfinexEventRegistration
    {
        public Action<BitfinexSocketTradingPairTick> Handler { get; set; }
    }

    public class BitfinexFundingPairTickerEventRegistration : BitfinexEventRegistration
    {
        public Action<BitfinexSocketFundingPairTick> Handler { get; set; }
    }

    public class BitfinexTradeEventRegistration : BitfinexEventRegistration
    {
        public Action<BitfinexTradeSimpleV2[]> Callback { get; set; }
    }
}
