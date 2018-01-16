using System;
using System.Collections.Generic;
using System.Threading;

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
        public string ChannelName { get; set; }

        /// <summary>
        /// Channel id returned by bitfinex
        /// </summary>
        public long ChannelId { get; set; }

        public ManualResetEvent CompleteEvent { get; } = new ManualResetEvent(false);

        private BitfinexError error;

        public BitfinexError Error
        {
            get => error;
            set
            {
                error = value;
                CompleteEvent.Set();
            }
        }

        private bool confirmed;

        public bool Confirmed
        {
            get => confirmed;
            set
            {
                confirmed = value;
                if (confirmed)
                {
                    CompleteEvent.Set();
                }
            }
        }
    }

    public class BitfinexWalletSnapshotEventRegistration: BitfinexEventRegistration
    {
        public Action<BitfinexWallet[]> Handler { get; set; }
    }

    public class BitfinexOrderSnapshotEventRegistration : BitfinexEventRegistration
    {
        public Action<BitfinexOrder[]> Handler { get; set; }
    }

    public class BitfinexPositionsSnapshotEventRegistration: BitfinexEventRegistration
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
        public Action<BitfinexTradeSimple[]> Callback { get; set; }
    }
}
