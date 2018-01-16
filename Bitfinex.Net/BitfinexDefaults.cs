using System;
using System.Collections.Generic;
using Bitfinex.Net.RateLimiter;

namespace Bitfinex.Net
{
    public static class BitfinexDefaults
    {
        internal static string ApiKey { get; private set; }
        internal static string ApiSecret { get; private set; }

        internal static int? MaxCallRetry { get; private set; }
        internal static List<IRateLimiter> RateLimiters { get; } = new List<IRateLimiter>();

        public static void SetDefaultApiCredentials(string apiKey, string apiSecret)
        {
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                throw new ArgumentException("Api key or secret empty");
            }

            ApiKey = apiKey;
            ApiSecret = apiSecret;
        }

        /// <summary>
        /// Sets the maximum times to retry a call when there is a server error
        /// </summary>
        /// <param name="retry">The maximum retries</param>
        public static void SetDefaultRetries(int retry)
        {
            MaxCallRetry = retry;
        }

        /// <summary>
        /// Adds a rate limiter for all new clients.
        /// </summary>
        /// <param name="rateLimiter">The ratelimiter</param>
        public static void AddDefaultRateLimiter(IRateLimiter rateLimiter)
        {
            RateLimiters.Add(rateLimiter);
        }

        /// <summary>
        /// Removes all rate limiters for future clients.
        /// </summary>
        public static void RemoveDefaultRateLimiters()
        {
            RateLimiters.Clear();
        }
    }
}
