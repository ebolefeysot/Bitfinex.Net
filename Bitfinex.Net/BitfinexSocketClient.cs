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
using Bitfinex.Net.Enum;
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

        private long regId = 0;

        private long NextRegistrationId()
        {
            return Interlocked.Increment(ref this.regId);
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

            if (!string.IsNullOrEmpty(apiKey) && encryptedSecret != null)
                Authenticate();
        }

        /// <summary>
        /// This callback is run for every message received on the socket
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void SocketMessage(object sender, MessageEventArgs args)
        {
            var dataObject = JToken.Parse(args.Data);
            switch (dataObject)
            {
                case JObject jObject:
                    log.Debug($"Received object message: {jObject}");
                    var evnt = System.Enum.Parse(typeof(ApiEventEnum), jObject["event"].ToString(), true);
                    switch (evnt)
                    {
                        case ApiEventEnum.Info:
                            HandleInfoEvent(dataObject.ToObject<BitfinexInfoNew>());
                            break;

                        case ApiEventEnum.Auth:
                            HandleAuthenticationEvent(dataObject.ToObject<BitfinexAuthenticationResponse>());
                            break;

                        case ApiEventEnum.Subscribed:
                            HandleSubscriptionEvent(dataObject.ToObject<BitfinexSubscribedNew>());
                            break;

                        case ApiEventEnum.Unsubscribed:
                            HandleUnSubscriptionEvent(dataObject.ToObject<BitfinexSocketUnsubscribedResponse>());
                            break;

                        case ApiEventEnum.Error:
                            HandleErrorEvent(dataObject.ToObject<BitfinexSocketErrorMessage>());
                            break;

                        case ApiEventEnum.Pong:
                            throw new NotImplementedException("pong");

                        default:
                            HandleUnhandledEvent(jObject);
                            break;
                    }

                    break;
                case JArray jArray:
                    log.Debug($"Received array message: {jArray}");
                    if (jArray[1].ToString() == "hb")
                    {
                        // Heartbeat, no need to do anything with that
                        return;
                    }

                    if (jArray[0].ToString() == "0")
                        HandleAccountEvent(jArray.ToObject<BitfinexSocketEvent>());
                    else
                        HandleChannelEvent(jArray);
                    break;
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
            authentication.Signature = ByteToString(encryptedSecret.ComputeHash(Encoding.ASCII.GetBytes(authentication.Payload)));

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

        private void HandleInfoEvent(BitfinexInfoNew info)
        {
            if (info.Version != 0)
                log.Debug($"API protocol version {info.Version}");

            //if (info.Code != 0)
            {
                // 20051 reconnect
                // 20060 maintanance, pause
                // 20061 maintanance end, resub
            }
        }

        /// <summary>
        /// A subscription response has arrived.
        /// </summary>
        /// <param name="subscription"></param>
        private void HandleSubscriptionEvent(BitfinexSubscribedNew subscription)
        {
            //look if we get a pending request for this subscription
            BitfinexEventRegistration pending;
            lock (eventListLock)
            {
                pending = eventRegistrations.SingleOrDefault(r => r.ChannelName == subscription.Channel && !r.Confirmed);
            }

            if (pending == null)
            {
                log.Warn("Received registration confirmation but have nothing pending?");
                return;
            }

            //save id returned by bitfinex
            pending.ChannelId = subscription.ChannelId;

            //Confirm the subscription
            pending.ResponseEvent.Set();
            log.Debug($"Subscription confirmed for channel {subscription.Channel}, Id: {subscription.ChannelId}");
        }

        private void HandleUnSubscriptionEvent(BitfinexSocketUnsubscribedResponse subscription)
        {
            //look if we get a subcription for this channel
            BitfinexEventRegistration pending;
            lock (eventListLock)
            {
                pending = eventRegistrations.SingleOrDefault(r => r.ChannelId == subscription.ChannelId && r.Confirmed);
            }

            if (pending == null)
            {
                //not found in registration list !?
                log.Warn($"No subscription found for channel {subscription.ChannelId}");
                return;
            }

            log.Debug($"Unsubscription confirmed for channel {pending.ChannelName}, Id: {subscription.ChannelId}");

            //remove channel
            lock (eventListLock)
            {
                eventRegistrations.Remove(pending);
            }
        }

        private void HandleErrorEvent(BitfinexSocketErrorMessage error)
        {
            log.Warn($"Bitfinex socket error: {error.ErrorCode} - {error.ErrorMessage}");
            BitfinexEventRegistration waitingRegistration;
            lock (eventListLock)
            {
                waitingRegistration = eventRegistrations.SingleOrDefault(e => !e.Confirmed);
            }

            if (waitingRegistration != null)
            {
                waitingRegistration.Error = new BitfinexError(error.ErrorCode, error.ErrorMessage);
            }
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
                        tradeEventRegistration.Callback(evnt[1].ToObject<BitfinexTradeSimple[]>());
                    }
                    else
                    {
                        if (evnt[1].ToString() == "te")
                        {
                            tradeEventRegistration.Callback(new[] { evnt[2].ToObject<BitfinexTradeSimple>() });
                        }
                    }
                    break;
            }
        }

        public long SubscribeToOrdersSnapshot(Action<BitfinexOrder[]> handler)
        {
            long id = NextRegistrationId();
            AddEventRegistration(new BitfinexOrderSnapshotEventRegistration
            {
                Id = id,
                ChannelName = ChannelEnum.Os,
                Handler = handler
            });

            return id;
        }

        public long SubscribeToWalletSnapshotEvent(Action<BitfinexWallet[]> handler)
        {
            long id = NextRegistrationId();
            AddEventRegistration(new BitfinexWalletSnapshotEventRegistration()
            {
                Id = id,
                ChannelName = ChannelEnum.Ws,
                Handler = handler
            });

            return id;
        }

        public long SubscribeToPositionsSnapshotEvent(Action<BitfinexPosition[]> handler)
        {
            long id = NextRegistrationId();
            AddEventRegistration(new BitfinexPositionsSnapshotEventRegistration()
            {
                Id = id,
                EventTypes = new List<string>() { PositionsSnapshotEvent },
                Handler = handler
            });

            return id;
        }

        public long SubscribeToFundingOffersSnapshotEvent(Action<BitfinexFundingOffer[]> handler)
        {
            long id = NextRegistrationId();
            AddEventRegistration(new BitfinexFundingOffersSnapshotEventRegistration()
            {
                Id = id,
                ChannelName = ChannelEnum.Fos,
                Handler = handler
            });

            return id;
        }

        public long SubscribeToFundingCreditsSnapshotEvent(Action<BitfinexFundingCredit[]> handler)
        {
            long id = NextRegistrationId();
            AddEventRegistration(new BitfinexFundingCreditsSnapshotEventRegistration()
            {
                Id = id,
                ChannelName = ChannelEnum.Fcs,
                Handler = handler
            });

            return id;
        }

        public long SubscribeToFundingLoansSnapshotEvent(Action<BitfinexFundingLoan[]> handler)
        {
            long id = NextRegistrationId();
            AddEventRegistration(new BitfinexFundingLoansSnapshotEventRegistration()
            {
                Id = id,
                ChannelName = ChannelEnum.Fls,
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
                    Id = NextRegistrationId(),
                    ChannelName = ChannelEnum.Ticker,
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
                    Id = NextRegistrationId(),
                    ChannelName = ChannelEnum.Ticker,
                    Handler = handler
                };
                AddEventRegistration(registration);

                Socket.Send(JsonConvert.SerializeObject(new BitfinexTickerSubscribeRequest(symbol)));

                return WaitSubscription(registration);
            }
        }

        /// <summary>
        /// Subscribe to a trade ticker flow. Ticks are received live.
        /// </summary>
        /// <remarks>First tick contains several data (newer first)</remarks>
        /// <param name="symbol">ex: btcusd</param>
        /// <param name="callback">Action to execute when a tick arrive</param>
        /// <returns></returns>
        public BitfinexApiSubscriptionResponse SubscribeToTrades(string symbol, Action<BitfinexTradeSimple[]> callback)
        {
            //create data to send to bitfinex for registration
            var registration = new BitfinexTradeEventRegistration()
            {
                Id = NextRegistrationId(),
                ChannelName = ChannelEnum.Trades,
                Callback = callback
            };

            lock (subscriptionLock)
            {
                AddEventRegistration(registration);
            }

            var data = JsonConvert.SerializeObject(new BitfinexTradeSubscribeRequest(symbol), Formatting.Indented);
            log.Trace(data);

            //send request
            Socket.Send(data);

            //wait response
            var response = (BitfinexApiSubscriptionResponse)WaitSubscriptionNew(registration);
            return response;
        }

        private BitfinexApiResult<long> WaitSubscription(BitfinexEventRegistration registration)
        {
            var sw = Stopwatch.StartNew();
            if (!registration.ResponseEvent.WaitOne(2000))
            {
                lock (eventListLock)
                {
                    eventRegistrations.Remove(registration);
                }
                return Fail<long>(BitfinexErrors.GetError(BitfinexErrorKey.SubscriptionNotConfirmed));
            }

            sw.Stop();
            log.Debug($"Wait took {sw.ElapsedMilliseconds}ms");

            if (registration.Confirmed)
            {
                return Success(registration.Id);
            }

            lock (eventListLock)
            {
                eventRegistrations.Remove(registration);
            }

            return Fail<long>(registration.Error);
        }

        /// <summary>
        /// Wait for a response to the subscribe request
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        private BitfinexSocketMessagesBase WaitSubscriptionNew(BitfinexEventRegistration registration)
        {
            //wait confirmation for 2s
            if (!registration.ResponseEvent.WaitOne(2000))
            {
                //no confirmation received, cancel subscription
                lock (eventListLock)
                {
                    eventRegistrations.Remove(registration);
                }
                throw new TimeoutException("No confirmation received, subscription is canceled");
            }

            if (registration.Status == EventStatusEnum.Failure)
            {
                lock (eventListLock)
                {
                    eventRegistrations.Remove(registration);
                }

                return new BitfinexApiFailureResponse
                {
                    Code = registration.Error.ErrorCode,
                    Event = registration.EventTypes.ToString(),
                    Message = registration.Error.ErrorMessage
                };
            }

            //Subscribtion confirmed
            registration.Status = EventStatusEnum.Subscribed;

            return new BitfinexApiSubscriptionResponse(this)
            {
                Event = registration.EventTypes.ToString(),
                Channel = registration.ChannelName,
                ChannelId = registration.ChannelId
            };
        }

        /// <summary>
        /// Wait for a response to the unsubscribe request.
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        private BitfinexSocketMessagesBase WaitUnSubscription(BitfinexEventRegistration registration)
        {
            if (!registration.ResponseEvent.WaitOne(2000))
            {
                //event not completed. Timeout.
                throw new TimeoutException("No confirmation received");
            }

            if (registration.Status == EventStatusEnum.Failure)
            {
                return new BitfinexApiFailureResponse
                {
                    Code = registration.Error.ErrorCode,
                    Event = registration.EventTypes.ToString(),
                    Message = registration.Error.ErrorMessage
                };
            }

            //operation successfull
            lock (eventListLock)
            {
                eventRegistrations.Remove(registration);
            }

            return new BitfinexApiUnsubscriptionResponse
            {
                Event = registration.EventTypes.ToString(),
                Status = registration.ChannelName,
                ChannelId = registration.ChannelId
            };
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
            {
                return eventRegistrations.OfType<T>();
            }
        }

        public BitfinexSocketMessagesBase Unsubscribe(int channelId)
        {
            //look for the previous subscription
            BitfinexEventRegistration registration;
            lock (eventListLock)
            {
                registration = eventRegistrations.FirstOrDefault(r => r.ChannelId == channelId && r.Status == EventStatusEnum.Subscribed);
            }

            if (registration == null)
            {
                //no subscribtion found
                throw new ArgumentException($"No subscription found for {channelId}");
            }

            //prepare request message
            var request = new BitfinexUnsubscribeRequest(channelId);
            var data = JsonConvert.SerializeObject(request, Formatting.Indented);

            Socket.Send(data);

            //wait response
            var response = (BitfinexApiSubscriptionResponse)WaitUnSubscription(registration);
            return response;
        }
    }
}