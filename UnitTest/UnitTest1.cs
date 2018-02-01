using Bitfinex.Net;
using System;
using Bitfinex.Net.Enum;
using Bitfinex.Net.Objects;
using Newtonsoft.Json;
using Xunit;

namespace UnitTest
{
    public class UnitTest1
    {
        [Fact]
        public async void TestMethod()
        {
            //BitfinexClient bfx = new BitfinexClient();
            //var res = bfx.GetTrades("BTCUSD");


            var result = "{\"mid\":\"11256.5\",\"bid\":\"11256.0\",\"ask\":\"11257.0\",\"last_price\":\"11259.0\",\"low\":\"11256.0\",\"high\":\"12050.0\",\"volume\":\"28614.17402661\",\"timestamp\":\"1517213008.2927725\"}";
            var r = JsonConvert.DeserializeObject<BitfinexMarketOverview>(result);

            //BuyOrder buy = new BuyOrder
            //{
            //    Symbol= "tbcusd",
            //    Amount = 1,
            //    Price = 12300,
            //    Type = OrderType2.ExchangeLimit
            //};

            //var res = bfx.PlaceOrder(buy);
            //var res = bfx.PlaceOrder("btcusd", 1, 13200, OrderSide.Buy, OrderType2.ExchangeTrailingStop);
        }
    }
}
