using System;
using System.Security.Cryptography;
using System.Text;
using Bitfinex.Net.Interfaces;
using Bitfinex.Net.Objects;

namespace Bitfinex.Net
{
    public abstract class BitfinexAbstractClient : IDisposable
    {
        protected string apiKey;
        protected HMACSHA384 encryptedSecret;
        internal ILogger log;

        protected BitfinexAbstractClient(ILogger logger = null)
        {
            log = logger;

            if (BitfinexDefaults.ApiKey != null && BitfinexDefaults.ApiSecret != null)
            {
                SetApiCredentials(BitfinexDefaults.ApiKey, BitfinexDefaults.ApiSecret);
            }
        }

        /// <summary>
        /// Sets the API Key and secret. Api credentials can be managed at https://bittrex.com/Manage#sectionApi
        /// </summary>
        /// <param name="key"></param>
        /// <param name="secret"></param>
        protected void SetApiCredentials(string key, string secret)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Api key empty");
            }

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new ArgumentException("Api secret empty");
            }

            apiKey = key;
            encryptedSecret = new HMACSHA384(Encoding.ASCII.GetBytes(secret));
        }

        /// <summary>
        /// Api call failed. Return an object with details.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="error">error message</param>
        /// <param name="extraInformation">More information</param>
        /// <returns></returns>
        protected BitfinexApiResult<T> Fail<T>(BitfinexError error, string extraInformation = null)
        {
            log?.Warn($"Call failed: {error.ErrorMessage}");
            var result = new BitfinexApiResult<T>
            {
                Error = error
            };

            if (extraInformation != null)
            {
                result.Error.ErrorMessage += Environment.NewLine + extraInformation;
            }
            return result;
        }

        /// <summary>
        /// Api call succeeded. Return an object with details.
        /// </summary>
        /// <typeparam name="T">Type of data to return</typeparam>
        /// <param name="data">Api operation id</param>
        /// <returns></returns>
        protected BitfinexApiResult<T> Success<T>(T data)
        {
            var result = new BitfinexApiResult<T>
            {
                Result = data,
                Success = true
            };
            return result;
        }

        /// <summary>
        /// Convert a byte array to a hex string
        /// </summary>
        /// <param name="buff"></param>
        /// <returns></returns>
        protected string ByteToString(byte[] buff)
        {
            var sbinary = "";
            foreach (byte t in buff)
            {
                sbinary += t.ToString("x2"); /* hex format */
            }
            return sbinary;
        }

        public void Dispose()
        {
            encryptedSecret?.Dispose();
        }
    }
}
