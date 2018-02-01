using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Bitfinex.Net.Objects;

namespace Bitfinex.Net.Exceptions
{
    [Serializable]
    public class BitfinexException : Exception
    {
        public BitfinexError BitfinexError { get; set; }

        public BitfinexException()
        {
        }

        public BitfinexException(BitfinexError bitfinexError, Exception inner) : base(inner.Message, inner)
        {
            BitfinexError = bitfinexError;
        }

        protected BitfinexException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Add more info to the message
        /// </summary>
        public override String Message => $"{base.Message}\n{BitfinexError.ErrorCode}: {BitfinexError.ErrorMessage}";
    }
}
