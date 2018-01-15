using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocketSharp;
using Bitfinex.Net.Objects.SocketObjets;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Bitfinex.Net.Objects;
using Bitfinex.Net.Errors;
using System.Diagnostics;
using System.Threading;
using Bitfinex.Net.Interfaces;

namespace Bitfinex.Net
{
    public class BitfinexSocketClient : BitfinexAbstractClient
    {
        private const string BaseAddress = "wss://api.bitfinex.com/ws/2";
        private const string AuthenticationSucces = "OK";

        private const string PositionsSnapshotEvent = "ps";
        private const string WalletsSnapshotEvent = "ws";
        private const string OrdersSnapshotEvent = "os";
        private const string FundingOffersSnapshotEvent = "fos";
        private const string FundingCreditsSnapshotEvent = "fcs";
        private const string FundingLoansSnapshotEvent = "fls";
        private const string ActiveTradesSnapshotEvent = "ats"; // OK?
        private const string HeartbeatEvent = "hb";

        private static WebSocket Socket;
        private static bool Authenticated;

        private List<BitfinexEventRegistration> eventRegistrations = new List<BitfinexEventRegistration>();

        private string nonce => Math.Round((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds * 10).ToString(CultureInfo.InvariantCulture);

        private object subscriptionLock = new object();
        private object registrationIdLock = new object();
        private object eventListLock = new object();

        private long regId;

        private long registrationId
        {
            get
            {
                lock (registrationIdLock)
                {
                    return ++regId;
                }
            }
        }

        public BitfinexSocketClient(string apiKey, string apiSecret, ILogger logger = null) : base(logger)
        {
            SetApiCredentials(apiKey, apiSecret);
        }

        public BitfinexSocketClient(ILogger logger = null) : base(logger)
        {
        }

        public void Connect()
        {
            lock (subscriptionLock)
            {
                Socket = new WebSocket(BaseAddress);
            }
            Socket.Log.Level = LogLevel.Info;
            Socket.OnClose += SocketClosed;
            Socket.OnError += SocketError;
            Socket.OnOpen += SocketOpened;
            Socket.OnMessage += SocketMessage;

            Socket.Connect();
        }

        private void SocketClosed(object sender, CloseEventArgs args)
        {
            log.Debug("Socket closed");
        }

        private void SocketError(object sender, ErrorEventArgs args)
        {
            log.Error($"Socket error: {args.Exception?.GetType().Name} - {args.Message}");
        }

        private void SocketOpened(object sender, EventArgs args)
        {
            log.Debug($"Socket opened");

            if (!string.IsNullOrEmpty(apiKey) && encryptor != null)
                Authenticate();
        }

        private void SocketMessage(object sender, MessageEventArgs args)
        {
            var dataObject = JToken.Parse(args.Data);
            if (dataObject is JObject)
            {
                log.Debug($"Received object message: {dataObject}");
                var evnt = dataObject["event"].ToString();
                if (evnt == "info")
                    HandleInfoEvent(dataObject.ToObject<BitfinexInfo>());
                else if (evnt == "auth")
                    HandleAuthenticationEvent(dataObject.ToObject<BitfinexAuthenticationResponse>());
                else if (evnt == "subscribed")
                    HandleSubscriptionEvent(dataObject.ToObject<BitfinexSubscriptionResponse>());
                else if (evnt == "error")
                    HandleErrorEvent(dataObject.ToObject<BitfinexSocketError>());
                else
                    HandleUnhandledEvent((JObject)dataObject);
            }
            else if (dataObject is JArray)
            {
                log.Debug($"Received array message: {dataObject}");
                if (dataObject[1].ToString() == "hb")
                {
                    // Heartbeat, no need to do anything with that
                    return;
                }

                if (dataObject[0].ToString() == "0")
                    HandleAccountEvent(dataObject.ToObject<BitfinexSocketEvent>());
                else
                    HandleChannelEvent((JArray)dataObject);
            }
        }

        private void Authenticate()
        {
            var n = nonce;
            var authentication = new BitfinexAuthentication()
            {
                Event = "auth",
                ApiKey = apiKey,
                Nonce = n,
                Payload = "AUTH" + n
            };
            authentication.Signature = ByteToString(encryptor.ComputeHash(Encoding.ASCII.GetBytes(authentication.Payload)));

            Socket.Send(JsonConvert.SerializeObject(authentication));
        }

        private void HandleAuthenticationEvent(BitfinexAuthenticationResponse response)
        {
            if (response.Status == AuthenticationSucces)
            {
                Authenticated = true;
                log.Debug($"Socket authentication successful, authentication id : {response.AuthenticationId}");
            }
            else
            {
                log.Warn($"Socket authentication failed. Status: {response.Status}, Error code: {response.ErrorCode}, Error message: {response.ErrorMessage}");
            }
        }

        private void HandleInfoEvent(BitfinexInfo info)
        {
            if (info.Version != 0)
                log.Debug($"API protocol version {info.Version}");

            if (info.Code != 0)
            {
                // 20051 reconnect
                // 20060 maintanance, pause
                // 20061 maintanance end, resub
            }
        }

        private void HandleSubscriptionEvent(BitfinexSubscriptionResponse subscription)
        {
            BitfinexEventRegistration pending;
            lock (eventListLock)
                pending = eventRegistrations.SingleOrDefault(r => r.ChannelName == subscription.ChannelName && !r.Confirmed);

            if (pending == null)
            {
                log.Warn("Received registration confirmation but have nothing pending?");
                return;
            }

            pending.ChannelId = subscription.ChannelId;
            pending.Confirmed = true;
            log.Debug($"Subscription confirmed for channel {subscription.ChannelName}, ID: {subscription.ChannelId}");
        }

        private void HandleErrorEvent(BitfinexSocketError error)
        {
            log.Warn($"Bitfinex socket error: {error.ErrorCode} - {error.ErrorMessage}");
            BitfinexEventRegistration waitingRegistration;
            lock (eventListLock)
                waitingRegistration = eventRegistrations.SingleOrDefault(e => !e.Confirmed);

            if (waitingRegistration != null)
                waitingRegistration.Error = new BitfinexError(error.ErrorCode, error.ErrorMessage);
        }

        private void HandleUnhandledEvent(JObject data)
        {
            log.Debug($"Received uknown event: { data }");
        }

        private void HandleAccountEvent(BitfinexSocketEvent evnt)
        {
            if (evnt.Event == WalletsSnapshotEvent)
            {
                var obj = evnt.Data.ToObject<BitfinexWallet[]>();
                foreach (var handler in GetRegistrationsOfType<BitfinexWalletSnapshotEventRegistration>())
                    handler.Handler(obj);
            }
            else if (evnt.Event == OrdersSnapshotEvent)
            {
                var obj = evnt.Data.ToObject<BitfinexOrder[]>();
                foreach (var handler in GetRegistrationsOfType<BitfinexOrderSnapshotEventRegistration>())
                    handler.Handler(obj);
            }
            else if (evnt.Event == PositionsSnapshotEvent)
            {
                var obj = evnt.Data.ToObject<BitfinexPosition[]>();
                foreach (var handler in GetRegistrationsOfType<BitfinexPositionsSnapshotEventRegistration>())
                    handler.Handler(obj);
            }
            else if (evnt.Event == FundingOffersSnapshotEvent)
            {
                var obj = evnt.Data.ToObject<BitfinexFundingOffer[]>();
                foreach (var handler in GetRegistrationsOfType<BitfinexFundingOffersSnapshotEventRegistration>())
                    handler.Handler(obj);
            }
            else if (evnt.Event == FundingCreditsSnapshotEvent)
            {
                var obj = evnt.Data.ToObject<BitfinexFundingCredit[]>();
                foreach (var handler in GetRegistrationsOfType<BitfinexFundingCreditsSnapshotEventRegistration>())
                    handler.Handler(obj);
            }
            else if (evnt.Event == FundingLoansSnapshotEvent)
            {
                var obj = evnt.Data.ToObject<BitfinexFundingLoan[]>();
                foreach (var handler in GetRegistrationsOfType<BitfinexFundingLoansSnapshotEventRegistration>())
                    handler.Handler(obj);
            }
            else if (evnt.Event == ActiveTradesSnapshotEvent)
            {
            }
            else
            {
                log.Warn($"Received unknown account event: {evnt.Event}, data: {evnt.Data}");
            }
        }

        private void HandleChannelEvent(JArray evnt)
        {
            BitfinexEventRegistration registration;
            lock (eventListLock)
            {
                registration = eventRegistrations.SingleOrDefault(s => s.ChannelId == (int)evnt[0]);
            }

            switch (registration)
            {
                case null:
                    log.Warn("Received event but have no registration");
                    return;

                case BitfinexTradingPairTickerEventRegistration eventRegistration:
                    eventRegistration.Handler(evnt[1].ToObject<BitfinexSocketTradingPairTick>());
                    break;

                case BitfinexFundingPairTickerEventRegistration tickerEventRegistration:
                    tickerEventRegistration.Handler(evnt[1].ToObject<BitfinexSocketFundingPairTick>());
                    break;

                case BitfinexTradeEventRegistration tradeEventRegistration:
                    if (evnt[1] is JArray)
                    {
                        tradeEventRegistration.Handler(evnt[1].ToObject<BitfinexTradeSimple[]>());
                    }
                    else
                    {
                        tradeEventRegistration.Handler(new[] { evnt[2].ToObject<BitfinexTradeSimple>() });
                    }
                    break;
            }
        }

        public long SubscribeToOrdersSnapshot(Action<BitfinexOrder[]> handler)
        {
            long id = registrationId;
            AddEventRegistration(new BitfinexOrderSnapshotEventRegistration()
            {
                Id = id,
                Confirmed = true,
                ChannelName = OrdersSnapshotEvent,
                Handler = handler
            });

            return id;
        }

        public long SubscribeToWalletSnapshotEvent(Action<BitfinexWallet[]> handler)
        {
            long id = registrationId;
            AddEventRegistration(new BitfinexWalletSnapshotEventRegistration()
            {
                Id = id,
                Confirmed = true,
                ChannelName = WalletsSnapshotEvent,
                Handler = handler
            });

            return id;
        }

        public long SubscribeToPositionsSnapshotEvent(Action<BitfinexPosition[]> handler)
        {
            long id = registrationId;
            AddEventRegistration(new BitfinexPositionsSnapshotEventRegistration()
            {
                Id = id,
                Confirmed = true,
                EventTypes = new List<string>() { PositionsSnapshotEvent },
                Handler = handler
            });

            return id;
        }

        public long SubscribeToFundingOffersSnapshotEvent(Action<BitfinexFundingOffer[]> handler)
        {
            long id = registrationId;
            AddEventRegistration(new BitfinexFundingOffersSnapshotEventRegistration()
            {
                Id = id,
                Confirmed = true,
                ChannelName = FundingOffersSnapshotEvent,
                Handler = handler
            });

            return id;
        }

        public long SubscribeToFundingCreditsSnapshotEvent(Action<BitfinexFundingCredit[]> handler)
        {
            long id = registrationId;
            AddEventRegistration(new BitfinexFundingCreditsSnapshotEventRegistration()
            {
                Id = id,
                Confirmed = true,
                ChannelName = FundingCreditsSnapshotEvent,
                Handler = handler
            });

            return id;
        }

        public long SubscribeToFundingLoansSnapshotEvent(Action<BitfinexFundingLoan[]> handler)
        {
            long id = registrationId;
            AddEventRegistration(new BitfinexFundingLoansSnapshotEventRegistration()
            {
                Id = id,
                Confirmed = true,
                ChannelName = FundingLoansSnapshotEvent,
                Handler = handler
            });

            return id;
        }

        public BitfinexApiResult<long> SubscribeToTradingPairTicker(string symbol, Action<BitfinexSocketTradingPairTick> handler)
        {
            lock (subscriptionLock)
            {
                var registration = new BitfinexTradingPairTickerEventRegistration()
                {
                    Id = registrationId,
                    ChannelName = "ticker",
                    Handler = handler
                };
                AddEventRegistration(registration);

                Socket.Send(JsonConvert.SerializeObject(new BitfinexTickerSubscribeRequest(symbol)));

                return WaitSubscription(registration);
            }
        }

        public BitfinexApiResult<long> SubscribeToFundingPairTicker(string symbol, Action<BitfinexSocketFundingPairTick> handler)
        {
            lock (subscriptionLock)
            {
                var registration = new BitfinexFundingPairTickerEventRegistration()
                {
                    Id = registrationId,
                    ChannelName = "ticker",
                    Handler = handler
                };
                AddEventRegistration(registration);

                Socket.Send(JsonConvert.SerializeObject(new BitfinexTickerSubscribeRequest(symbol)));

                return WaitSubscription(registration);
            }
        }

        /// <summary>
        /// Subscribe to a ticker flow. Ticks are received live.
        /// </summary>
        /// <remarks>First tick contains several data (newer first)</remarks>
        /// <param name="symbol">ex: btcusd</param>
        /// <param name="handler">Callback</param>
        /// <returns></returns>
        public BitfinexApiResult<long> SubscribeToTrades(string symbol, Action<BitfinexTradeSimple[]> handler)
        {
            lock (subscriptionLock)
            {
                //create data to send to bitfinex for registration
                var registration = new BitfinexTradeEventRegistration()
                {
                    Id = registrationId,
                    ChannelName = "trades",
                    Handler = handler
                };
                AddEventRegistration(registration);

                Socket.Send(JsonConvert.SerializeObject(new BitfinexTradeSubscribeRequest(symbol)));

                return WaitSubscription(registration);
            }
        }

        private BitfinexApiResult<long> WaitSubscription(BitfinexEventRegistration registration)
        {
            var sw = Stopwatch.StartNew();
            if (!registration.CompleteEvent.WaitOne(2000))
            {
                lock (eventListLock)
                    eventRegistrations.Remove(registration);
                return ThrowErrorMessage<long>(BitfinexErrors.GetError(BitfinexErrorKey.SubscriptionNotConfirmed));
            }
            sw.Stop();
            log.Debug($"Wait took {sw.ElapsedMilliseconds}ms");

            if (registration.Confirmed)
                return ReturnResult(registration.Id);

            lock (eventListLock)
            {
                eventRegistrations.Remove(registration);
            }

            return ThrowErrorMessage<long>(registration.Error);
        }

        private void AddEventRegistration(BitfinexEventRegistration reg)
        {
            lock (eventListLock)
            {
                eventRegistrations.Add(reg);
            }
        }

        private IEnumerable<T> GetRegistrationsOfType<T>()
        {
            lock (eventListLock)
                return eventRegistrations.OfType<T>();
        }
    }
}
