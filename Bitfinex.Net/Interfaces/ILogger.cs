using System;

namespace Bitfinex.Net.Interfaces
{
    public interface ILogger
    {
        void Trace(string message);

        void Debug(string message);

        void Warn(string message);

        void Error(string message, Exception e = null);
    }
}