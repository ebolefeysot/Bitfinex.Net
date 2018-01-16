using System;
using System.IO;

namespace Bitfinex.Net.Logging
{
    internal class Log : ILogger
    {
        public TextWriter TextWriter { get; set; } = new TraceTextWriter();
        public LogVerbosity Level { get; set; } = LogVerbosity.Warning;

        public void Write(LogVerbosity logType, string message)
        {
            if ((int)logType >= (int)Level)
                TextWriter.WriteLine($"{DateTime.Now:hh:mm:ss:fff} | {logType} | {message}");
        }

        public void Warn(string message)
        {
            Write(LogVerbosity.Warning, message);
        }

        public void Error(string message, Exception e = null)
        {
            throw new NotImplementedException();
        }

        public void Trace(string message)
        {
            throw new NotImplementedException();
        }

        public void Debug(string message)
        {
            Write(LogVerbosity.Debug, message);
        }

        public void Error(string message)
        {
            Write(LogVerbosity.Error, message);
        }
    }

    public enum LogVerbosity
    {
        Debug,
        Warning,
        Error
    }
}
