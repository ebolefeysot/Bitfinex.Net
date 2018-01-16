using System;
using System.IO;

namespace Bitfinex.Net.Logging
{
    internal interface ILogger
    {
        TextWriter TextWriter { get; set; }
        LogVerbosity Level { get; set; }

        void Trace(string message);

        void Debug(string message);

        void Warn(string message);

        void Error(string message, Exception e = null);
    }
}