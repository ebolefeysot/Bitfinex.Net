using System;
using System.IO;
using Bitfinex.Net.Interfaces;

namespace ConsoleApp
{
    internal class Log : ILogger
    {
        //todo nlog to console
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
            Write(LogVerbosity.Error, $"{message}\n{e?.Message}");
        }

        public void Trace(string message)
        {
            Write(LogVerbosity.Trace, message);
        }

        public void Debug(string message)
        {
            Write(LogVerbosity.Debug, message);
        }
    }

    public enum LogVerbosity
    {
        Debug,
        Warning,
        Error,
        Trace
    }
}
