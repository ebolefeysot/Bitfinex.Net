﻿using System;
using System.Linq;
using Bitfinex.Net;
using Bitfinex.Net.Enum;
using Bitfinex.Net.Interfaces;
using Bitfinex.Net.Objects;
using Bitfinex.Net.Objects.SocketObjets;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            ILogger log = new Log();

            var bfx = new BitfinexSocketClient(log);

            //connect
            bfx.Connect();

            //subscribe
            BitfinexApiSubscriptionResponse sub = bfx.SubscribeToTrades("btcusd", TradesTicker);

            //wait
            Console.ReadLine();

            //unsubscribe
            var res = sub.Unsubscribe();
        }

        /// <summary>
        /// Ticker callback
        /// </summary>
        /// <param name="tick"></param>
        private static void TradesTicker(BitfinexTradeSimpleV2[] tick)
        {
            //first tick contains several data (newer first)
            foreach (var t in tick.Reverse())
            {
                Console.BackgroundColor = t.Type == TypeEnum.Sell ? ConsoleColor.DarkRed : ConsoleColor.DarkGreen;
                Console.WriteLine(string.Format($"Id: {t.Id} Date: {t.Timestamp.ToLocalTime()} Price: {t.Price}, vol: {t.Amount:+0.########;-0.########}"));
            }
        }
    }
}
