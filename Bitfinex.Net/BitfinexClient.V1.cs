using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Bitfinex.Net.Converters;
using Bitfinex.Net.Enum;
using Bitfinex.Net.Errors;
using Bitfinex.Net.Objects;
using Newtonsoft.Json;

namespace Bitfinex.Net
{
    public partial class BitfinexClient
    {
        private const string SymbolsEndpoint = "symbols";
        private const string WalletsV1Endpoint = "balances";
        private const string TickerEndpoint = "pubticker/{}";
        private const string SymbolDetailsEndpoint = "symbols_details";
        private const string AccountInfosEndpoint = "account_infos";
        private const string AccountFeesEndpoint = "account_fees";
        private const string AccountSummaryEndpoint = "summary";
        private const string DepositAddressEndpoint = "deposit/new";
        private const string KeyPermissionsEndpoint = "key_info";
        private const string MarginInfoEndpoint = "margin_infos";
        private const string WithdrawEndpoint = "withdraw";
        private const string NewOrderEndpoint = "order/new";
        private const string CancelOrderEndpoint = "order/cancel";
        private const string CancelAllOrdersEndpoint = "order/cancel/all";
        private const string OrderStatusEndpoint = "order/status";
        private const string ClaimPositionEndpoint = "position/claim";
        private const string BalanceHistoryEndpoint = "history";
        private const string WithdrawalDepositHistoryEndpoint = "history/movements";
        private const string NewOfferEndpoint = "offer/new";
        private const string CancelOfferEndpoint = "offer/cancel";
        private const string OfferStatusEndpoint = "offer/status";
        private const string ActiveFundingUsedEndpoint = "taken_funds";
        private const string ActiveFundingNotUsedEndpoint = "unused_taken_funds";
        private const string TotalTakenFundsEndpoint = "total_taken_funds";
        private const string CloseMarginFundingEndpoint = "funding/close";
        private const string BasketManageEndpoint = "basket_manage";


        public BitfinexApiResult<string[]> GetSymbols() => GetSymbolsAsync().Result;
        public async Task<BitfinexApiResult<string[]>> GetSymbolsAsync()
        {
            return await ExecutePublicRequest<string[]>(GetUrl(SymbolsEndpoint, ApiVersion1), GetMethod);
        }

        public BitfinexApiResult<BitfinexSymbol[]> GetSymbolDetails() => GetSymbolDetailsAsync().Result;
        public async Task<BitfinexApiResult<BitfinexSymbol[]>> GetSymbolDetailsAsync()
        {
            return await ExecutePublicRequest<BitfinexSymbol[]>(GetUrl(SymbolDetailsEndpoint, ApiVersion1), GetMethod);
        }

        public BitfinexApiResult<BitfinexAccountInfo> GetAccountInfo() => GetAccountInfoAsync().Result;
        public async Task<BitfinexApiResult<BitfinexAccountInfo>> GetAccountInfoAsync()
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexAccountInfo>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            var result = await ExecuteAuthenticatedRequestV1<BitfinexAccountInfo[]>(GetUrl(AccountInfosEndpoint, ApiVersion1), PostMethod);
            if (result.Success)
                return Success(result.Result[0]);
            return Fail<BitfinexAccountInfo>(result.Error);
        }

        public BitfinexApiResult<BitfinexAccountFee> GetAccountFees() => GetAccountFeesAsync().Result;
        public async Task<BitfinexApiResult<BitfinexAccountFee>> GetAccountFeesAsync()
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexAccountFee>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            return await ExecuteAuthenticatedRequestV1<BitfinexAccountFee>(GetUrl(AccountFeesEndpoint, ApiVersion1), PostMethod);
        }

        public BitfinexApiResult<BitfinexAccountSummary> GetAccountSummary() => GetAccountSummaryAsync().Result;
        public async Task<BitfinexApiResult<BitfinexAccountSummary>> GetAccountSummaryAsync()
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexAccountSummary>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            return await ExecuteAuthenticatedRequestV1<BitfinexAccountSummary>(GetUrl(AccountSummaryEndpoint, ApiVersion1), PostMethod);
        }

        public BitfinexApiResult<BitfinexDepositAddress> GetDepositAddress(DepositMethod method, WalletType wallet, bool? renew = null) => GetDepositAddressAsync(method, wallet, renew).Result;
        public async Task<BitfinexApiResult<BitfinexDepositAddress>> GetDepositAddressAsync(DepositMethod method, WalletType wallet, bool? renew = null)
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexDepositAddress>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            var parameters = new Dictionary<string, object>()
            {
                { "method", JsonConvert.SerializeObject(method, new DepositMethodConverter(false)) },
                { "wallet_name",  JsonConvert.SerializeObject(wallet, new WalletTypeConverter(false)) }
            };

            AddOptionalParameter(parameters, "renew", renew?.ToString());

            var result = await ExecuteAuthenticatedRequestV1<BitfinexDepositAddress>(GetUrl(DepositAddressEndpoint, ApiVersion1), PostMethod, parameters);
            if (result.Error != null)
                return Fail<BitfinexDepositAddress>(result.Error);
            if (result.Result.Result != "success")
                return Fail<BitfinexDepositAddress>(BitfinexErrors.GetError(BitfinexErrorKey.DepositAddressFailed), result.Result.Result);
            return Success(result.Result);
        }

        public BitfinexApiResult<BitfinexApiKeyPermissions> GetApiKeyPermissions() => GetApiKeyPermissionsAsync().Result;
        public async Task<BitfinexApiResult<BitfinexApiKeyPermissions>> GetApiKeyPermissionsAsync()
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexApiKeyPermissions>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            return await ExecuteAuthenticatedRequestV1<BitfinexApiKeyPermissions>(GetUrl(KeyPermissionsEndpoint, ApiVersion1), PostMethod);
        }

        public BitfinexApiResult<BitfinexMarginInfo[]> GetMarginInformation() => GetMarginInformationAsync().Result;
        public async Task<BitfinexApiResult<BitfinexMarginInfo[]>> GetMarginInformationAsync()
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexMarginInfo[]>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            return await ExecuteAuthenticatedRequestV1<BitfinexMarginInfo[]>(GetUrl(MarginInfoEndpoint, ApiVersion1), PostMethod);
        }

        public BitfinexApiResult<BitfinexWithdrawResult> WithdrawCrypto(WithdrawType withdrawType, WalletType2 walletType, double amount, string address, string paymentId = null) => WithdrawCryptoAsync(withdrawType, walletType, amount, address, paymentId).Result;
        public async Task<BitfinexApiResult<BitfinexWithdrawResult>> WithdrawCryptoAsync(WithdrawType withdrawType, WalletType2 walletType, double amount, string address, string paymentId = null)
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexWithdrawResult>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            var parameters = new Dictionary<string, object>()
            {
                { "withdraw_type", JsonConvert.SerializeObject(withdrawType, new WithdrawTypeConverter(false)) },
                { "walletselected",  JsonConvert.SerializeObject(walletType, new WalletType2Converter(false)) },
                { "amount", amount.ToString(CultureInfo.InvariantCulture) },
                { "address", address }
            };
            AddOptionalParameter(parameters, "payment_id", paymentId);

            var result = await ExecuteAuthenticatedRequestV1<BitfinexWithdrawResult[]>(GetUrl(WithdrawEndpoint, ApiVersion1), PostMethod, parameters);
            if (result.Error != null)
                return Fail<BitfinexWithdrawResult>(result.Error);
            if (result.Result[0].Status != "success")
                return Fail<BitfinexWithdrawResult>(BitfinexErrors.GetError(BitfinexErrorKey.WithdrawFailed), result.Result[0].Message);
            return Success(result.Result[0]);
        }

        public BitfinexApiResult<BitfinexWithdrawResult> WithdrawWire(WithdrawType withdrawType,
            WalletType2 walletType,
            double amount,
            string accountNumber,
            string bankName,
            string bankAddress,
            string bankCity,
            string bankCountry,
            string accountName = null,
            string swiftCode = null,
            string detailPayment = null,
            bool? useExpressWire = null,
            string intermediaryBankName = null,
            string intermediaryBankAddress = null,
            string intermediaryBankCity = null,
            string intermediaryBankCountry = null,
            string intermediaryBankAccount = null,
            string intermediaryBankSwift = null) => WithdrawCryptoAsync(withdrawType, walletType, amount, accountNumber, bankName, bankAddress, bankCity, bankCountry, accountName,
                swiftCode, detailPayment, useExpressWire, intermediaryBankName, intermediaryBankAddress, intermediaryBankCity, intermediaryBankAccount, intermediaryBankSwift).Result;
        public async Task<BitfinexApiResult<BitfinexWithdrawResult>> WithdrawCryptoAsync(WithdrawType withdrawType,
            WalletType2 walletType,
            double amount,
            string accountNumber,
            string bankName,
            string bankAddress,
            string bankCity,
            string bankCountry,
            string accountName = null,
            string swiftCode = null,
            string detailPayment = null,
            bool? useExpressWire = null,
            string intermediaryBankName = null,
            string intermediaryBankAddress = null,
            string intermediaryBankCity = null,
            string intermediaryBankCountry = null,
            string intermediaryBankAccount = null,
            string intermediaryBankSwift = null)
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexWithdrawResult>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            var parameters = new Dictionary<string, object>()
            {
                { "withdraw_type", JsonConvert.SerializeObject(withdrawType, new WithdrawTypeConverter(false)) },
                { "walletselected",  JsonConvert.SerializeObject(walletType, new WalletType2Converter(false)) },
                { "amount", amount.ToString(CultureInfo.InvariantCulture) },
                { "account_number", accountNumber },
                { "bank_name", bankName },
                { "bank_address", bankAddress },
                { "bank_city", bankCity },
                { "bank_country", bankCountry }
            };
            AddOptionalParameter(parameters, "account_name", accountName);
            AddOptionalParameter(parameters, "swift", swiftCode);
            AddOptionalParameter(parameters, "detail_payment", detailPayment);
            AddOptionalParameter(parameters, "expressWire", useExpressWire != null ? JsonConvert.SerializeObject(useExpressWire, new BoolToIntConverter(false)) : null);
            AddOptionalParameter(parameters, "intermediary_bank_name", intermediaryBankName);
            AddOptionalParameter(parameters, "intermediary_bank_address", intermediaryBankAddress);
            AddOptionalParameter(parameters, "intermediary_bank_city", intermediaryBankCity);
            AddOptionalParameter(parameters, "intermediary_bank_country", intermediaryBankCountry);
            AddOptionalParameter(parameters, "intermediary_bank_account", intermediaryBankAccount);
            AddOptionalParameter(parameters, "intermediary_bank_swift", intermediaryBankSwift);

            var result = await ExecuteAuthenticatedRequestV1<BitfinexWithdrawResult[]>(GetUrl(WithdrawEndpoint, ApiVersion1), PostMethod, parameters);
            if (result.Error != null)
                return Fail<BitfinexWithdrawResult>(result.Error);
            if (result.Result[0].Status != "success")
                return Fail<BitfinexWithdrawResult>(BitfinexErrors.GetError(BitfinexErrorKey.WithdrawFailed), result.Result[0].Message);
            return Success(result.Result[0]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="amount">Order size: how much you want to buy or sell</param>
        /// <param name="price">Price to buy or sell at. Must be positive. Use random number for market orders</param>
        /// <param name="side">Either buy or sell</param>
        /// <param name="type">Type of trading orders</param>
        /// <param name="hidden">true if the order should be hidden</param>
        /// <param name="postOnly">true if the order should be post only. Only relevant for limit orders.</param>
        /// <param name="useAllAvailable">true will post an order that will use all of your available balance.</param>
        /// <param name="ocoOrder">Set an additional STOP OCO order that will be linked with the current order.</param>
        /// <param name="buyPriceOco">If ocoorder is true, this field represent the price of the OCO stop order to place</param>
        /// <param name="sellPriceOco">If ocoorder is true, this field represent the price of the OCO stop order to place</param>
        /// <returns></returns>
        public BitfinexApiResult<BitfinexPlacedOrder> PlaceOrder(
            string symbol,
            double amount,
            double price,
            OrderSide side,
            OrderType2 type,
            bool? hidden = null,
            bool? postOnly = null,
            bool? useAllAvailable = null,
            bool? ocoOrder = null,
            double? buyPriceOco = null,
            double? sellPriceOco = null)
        {
            return PlaceOrderAsync(symbol, amount, price, side, type, hidden, postOnly, useAllAvailable, ocoOrder, buyPriceOco, sellPriceOco).Result;
        }

        public async Task<BitfinexApiResult<BitfinexPlacedOrder>> PlaceOrderAsync(string symbol,
            double amount,
            double price,
            OrderSide side,
            OrderType2 type,
            bool? hidden = null,
            bool? postOnly = null,
            bool? useAllAvailable = null,
            bool? ocoOrder = null,
            double? buyPriceOco = null,
            double? sellPriceOco = null)
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
            {
                return Fail<BitfinexPlacedOrder>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));
            }

            var parameters = new Dictionary<string, object>
            {
                { "symbol", symbol },
                { "amount", amount.ToString(CultureInfo.InvariantCulture) },
                { "price", price.ToString(CultureInfo.InvariantCulture) },
                { "exchange", "bitfinex" },
                { "side", JsonConvert.SerializeObject(side, new OrderSideConverter(false)) },
                { "type", JsonConvert.SerializeObject(type, new OrderType2Converter(false)) },
            };

            AddOptionalParameter(parameters, "is_hidden", hidden?.ToString());
            AddOptionalParameter(parameters, "is_postonly", postOnly?.ToString());
            AddOptionalParameter(parameters, "use_all_available", useAllAvailable != null ? JsonConvert.SerializeObject(useAllAvailable, new BoolToIntConverter(false)) : null);
            AddOptionalParameter(parameters, "ocoorder", ocoOrder?.ToString());
            AddOptionalParameter(parameters, "buy_price_oco", buyPriceOco?.ToString(CultureInfo.InvariantCulture));
            AddOptionalParameter(parameters, "sell_price_oco", sellPriceOco?.ToString(CultureInfo.InvariantCulture));

            return await ExecuteAuthenticatedRequestV1<BitfinexPlacedOrder>(GetUrl(NewOrderEndpoint, ApiVersion1), PostMethod, parameters);
        }

        public BitfinexApiResult<BitfinexBaseOrder> CancelOrder(long orderId) => CancelOrderAsync(orderId).Result;
        public async Task<BitfinexApiResult<BitfinexBaseOrder>> CancelOrderAsync(long orderId)
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexBaseOrder>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            var parameters = new Dictionary<string, object>()
            {
                {"order_id", orderId},
            };
            return await ExecuteAuthenticatedRequestV1<BitfinexBaseOrder>(GetUrl(CancelOrderEndpoint, ApiVersion1), PostMethod, parameters);
        }

        public BitfinexApiResult<BitfinexResponseMessage> CancelAllOrders() => CancelAllOrdersAsync().Result;
        public async Task<BitfinexApiResult<BitfinexResponseMessage>> CancelAllOrdersAsync()
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexResponseMessage>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            return await ExecuteAuthenticatedRequestV1<BitfinexResponseMessage>(GetUrl(CancelAllOrdersEndpoint, ApiVersion1), PostMethod);
        }

        public BitfinexApiResult<BitfinexBaseOrder> GetOrderStatus(long orderId) => GetOrderStatusAsync(orderId).Result;
        public async Task<BitfinexApiResult<BitfinexBaseOrder>> GetOrderStatusAsync(long orderId)
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexBaseOrder>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            var parameters = new Dictionary<string, object>()
            {
                {"order_id", orderId},
            };
            return await ExecuteAuthenticatedRequestV1<BitfinexBaseOrder>(GetUrl(OrderStatusEndpoint, ApiVersion1), PostMethod, parameters);
        }

        public BitfinexApiResult<BitfinexClaimedPosition> ClaimPosition(long positionId, double amount) => ClaimPositionAsync(positionId, amount).Result;
        public async Task<BitfinexApiResult<BitfinexClaimedPosition>> ClaimPositionAsync(long positionId, double amount)
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexClaimedPosition>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            var parameters = new Dictionary<string, object>()
            {
                {"position_id", positionId},
                {"amount", amount},
            };
            return await ExecuteAuthenticatedRequestV1<BitfinexClaimedPosition>(GetUrl(ClaimPositionEndpoint, ApiVersion1), PostMethod, parameters);
        }

        public BitfinexApiResult<BitfinexBalanceChange[]> GetBalanceHistory(string currency, DateTime? since = null, DateTime? until = null, int? limit = null, WalletType2? wallet = null) => GetBalanceHistoryAsync(currency, since, until, limit, wallet).Result;
        public async Task<BitfinexApiResult<BitfinexBalanceChange[]>> GetBalanceHistoryAsync(string currency, DateTime? since = null, DateTime? until = null, int? limit = null, WalletType2? wallet = null)
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexBalanceChange[]>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            var parameters = new Dictionary<string, object>()
            {
                {"currency", currency},
            };
            AddOptionalParameter(parameters, "since", since != null ? JsonConvert.SerializeObject(since, new TimestampSecondsConverter(false)) : null);
            AddOptionalParameter(parameters, "until", until != null ? JsonConvert.SerializeObject(until, new TimestampSecondsConverter(false)) : null);
            AddOptionalParameter(parameters, "limit", limit);
            AddOptionalParameter(parameters, "wallet", since != null ? JsonConvert.SerializeObject(since, new WalletType2Converter(false)) : null);
            return await ExecuteAuthenticatedRequestV1<BitfinexBalanceChange[]>(GetUrl(BalanceHistoryEndpoint, ApiVersion1), PostMethod, parameters);
        }

        public BitfinexApiResult<BitfinexDepositWithdrawal[]> GetWithdrawDepositHistory(string currency, WithdrawType? type = null, DateTime? since = null, DateTime? until = null, int? limit = null) => GetWithdrawDepositHistoryAsync(currency, type, since, until, limit).Result;
        public async Task<BitfinexApiResult<BitfinexDepositWithdrawal[]>> GetWithdrawDepositHistoryAsync(string currency, WithdrawType? type = null, DateTime? since = null, DateTime? until = null, int? limit = null)
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexDepositWithdrawal[]>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            var parameters = new Dictionary<string, object>()
            {
                {"currency", currency},
            };
            AddOptionalParameter(parameters, "since", since != null ? JsonConvert.SerializeObject(since, new TimestampSecondsConverter(false)) : null);
            AddOptionalParameter(parameters, "until", until != null ? JsonConvert.SerializeObject(until, new TimestampSecondsConverter(false)) : null);
            AddOptionalParameter(parameters, "limit", limit);
            AddOptionalParameter(parameters, "method", type != null ? JsonConvert.SerializeObject(type, new WithdrawTypeConverter(false)) : null);

            return await ExecuteAuthenticatedRequestV1<BitfinexDepositWithdrawal[]>(GetUrl(WithdrawalDepositHistoryEndpoint, ApiVersion1), PostMethod, parameters);
        }

        public BitfinexApiResult<BitfinexOffer> PlaceNewOffer(string currency, double amount, double rate, int period, FundingType type) => PlaceNewOfferAsync(currency, amount, rate, period, type).Result;
        public async Task<BitfinexApiResult<BitfinexOffer>> PlaceNewOfferAsync(string currency, double amount, double rate, int period, FundingType type)
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexOffer>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            var parameters = new Dictionary<string, object>()
            {
                {"currency", currency},
                { "amount", amount},
                { "rate", rate},
                { "period", period},
                { "direction", JsonConvert.SerializeObject(type, new FundingTypeConverter(false))}
            };

            return await ExecuteAuthenticatedRequestV1<BitfinexOffer>(GetUrl(NewOfferEndpoint, ApiVersion1), PostMethod, parameters);
        }

        public BitfinexApiResult<BitfinexOffer> CancelOffer(long offerId) => CancelOfferAsync(offerId).Result;
        public async Task<BitfinexApiResult<BitfinexOffer>> CancelOfferAsync(long offerId)
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexOffer>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            var parameters = new Dictionary<string, object>()
            {
                {"offer_id", offerId},
            };
            return await ExecuteAuthenticatedRequestV1<BitfinexOffer>(GetUrl(CancelOfferEndpoint, ApiVersion1), PostMethod, parameters);
        }

        public BitfinexApiResult<BitfinexOffer> GetOfferStatus(long offerId) => GetOfferStatusAsync(offerId).Result;
        public async Task<BitfinexApiResult<BitfinexOffer>> GetOfferStatusAsync(long offerId)
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexOffer>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            var parameters = new Dictionary<string, object>()
            {
                {"offer_id", offerId},
            };
            return await ExecuteAuthenticatedRequestV1<BitfinexOffer>(GetUrl(OfferStatusEndpoint, ApiVersion1), PostMethod, parameters);
        }

        public BitfinexApiResult<BitfinexActiveMarginFund[]> GetActiveFundingUsed() => GetActiveFundingUsedAsync().Result;
        public async Task<BitfinexApiResult<BitfinexActiveMarginFund[]>> GetActiveFundingUsedAsync()
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexActiveMarginFund[]>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            return await ExecuteAuthenticatedRequestV1<BitfinexActiveMarginFund[]>(GetUrl(ActiveFundingUsedEndpoint, ApiVersion1), PostMethod);
        }

        public BitfinexApiResult<BitfinexActiveMarginFund[]> GetActiveFundingNotUsed() => GetActiveFundingNotUsedAsync().Result;
        public async Task<BitfinexApiResult<BitfinexActiveMarginFund[]>> GetActiveFundingNotUsedAsync()
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexActiveMarginFund[]>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            return await ExecuteAuthenticatedRequestV1<BitfinexActiveMarginFund[]>(GetUrl(ActiveFundingNotUsedEndpoint, ApiVersion1), PostMethod);
        }

        public BitfinexApiResult<BitfinexTakenFund[]> GetTotalTakenFunds() => GetTotalTakenFundsAsync().Result;
        public async Task<BitfinexApiResult<BitfinexTakenFund[]>> GetTotalTakenFundsAsync()
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexTakenFund[]>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            return await ExecuteAuthenticatedRequestV1<BitfinexTakenFund[]>(GetUrl(TotalTakenFundsEndpoint, ApiVersion1), PostMethod);
        }

        /// <summary>
        /// Get a list of the most recent trades for the given symbol.
        /// </summary>
        /// <param name="market"></param>
        /// <param name="limit">Limit the number of trades returned.Must be >= 1. Default 50.</param>
        /// <param name="startTime">Only show trades at or after this timestamp (not working on bitfinex...)</param>
        /// <returns></returns>
        public BitfinexApiResult<BitfinexTradeSimple[]> GetTrades(string market, int? limit = null) => GetTradesAsync(market, limit).Result;

        /// <summary>
        /// Get a list of the most recent trades for the given symbol.
        /// </summary>
        /// <param name="market"></param>
        /// <param name="limit">Limit the number of trades returned.Must be >= 1. Default 50.</param>
        /// <param name="startTime">Only show trades at or after this timestamp (not working on bitfinex...)</param>
        /// <returns></returns>
        public async Task<BitfinexApiResult<BitfinexTradeSimple[]>> GetTradesAsync(string market, int? limit = null)//, DateTime? startTime = null)
        {
            var parameters = new Dictionary<string, string>();
            AddOptionalParameter(parameters, "limit_trades", limit?.ToString());
            //AddOptionalParameter(parameters, "datetime", startTime != null ? JsonConvert.SerializeObject(startTime, new TimestampConverter(false)) : null);

            var url = GetUrl(FillPathParameter(TradesV1Endpoint, market), ApiVersion1, parameters);
            return await ExecutePublicRequest<BitfinexTradeSimple[]>(url, GetMethod);
        }

        public BitfinexApiResult<BitfinexActiveMarginFund> CloseMarginFunding(long swapId) => CloseMarginFundingAsync(swapId).Result;
        public async Task<BitfinexApiResult<BitfinexActiveMarginFund>> CloseMarginFundingAsync(long swapId)
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexActiveMarginFund>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            var parameters = new Dictionary<string, object>()
            {
                {"swap_id", swapId},
            };
            return await ExecuteAuthenticatedRequestV1<BitfinexActiveMarginFund>(GetUrl(CloseMarginFundingEndpoint, ApiVersion1), PostMethod, parameters);
        }

        public BitfinexApiResult<BitfinexErrorResult[]> BasketManage(double amount, SplitMerge splitMerge, string tokenName) => BasketManageAsync(amount, splitMerge, tokenName).Result;
        public async Task<BitfinexApiResult<BitfinexErrorResult[]>> BasketManageAsync(double amount, SplitMerge splitMerge, string tokenName)
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexErrorResult[]>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            var parameters = new Dictionary<string, object>()
            {
                {"amount", amount.ToString(CultureInfo.InvariantCulture)},
                {"dir", int.Parse(JsonConvert.SerializeObject(splitMerge, new SplitMergeConverter(false)))},
                {"name", tokenName},
            };
            return await ExecuteAuthenticatedRequestV1<BitfinexErrorResult[]>(GetUrl(BasketManageEndpoint, ApiVersion1), PostMethod, parameters);
        }

        public BitfinexApiResult<BitfinexMarketOverview> GetTicker(string market) => GetTickerAsync(market).Result;
        public async Task<BitfinexApiResult<BitfinexMarketOverview>> GetTickerAsync(string market)
        {
            return await ExecutePublicRequest<BitfinexMarketOverview>(GetUrl(FillPathParameter(TickerEndpoint, market), ApiVersion1), GetMethod);
        }

        public BitfinexApiResult<BitfinexWallet[]> GetWallets() => GetWalletsAsync().Result;
        public async Task<BitfinexApiResult<BitfinexWallet[]>> GetWalletsAsync()
        {
            if (string.IsNullOrEmpty(apiKey) || encryptedSecret == null)
                return Fail<BitfinexWallet[]>(BitfinexErrors.GetError(BitfinexErrorKey.NoApiCredentialsProvided));

            return await ExecuteAuthenticatedRequestV1<BitfinexWallet[]>(GetUrl(WalletsV1Endpoint, ApiVersion1), PostMethod);
        }
    }
}
