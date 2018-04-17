using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NuGet.Common;

namespace Etg.Yams.NuGet.Storage
{
    public class TraceLogger : ILogger
    {
        public void Log(LogLevel level, string data)
        {
            switch(level)
            {
                case LogLevel.Error:
                    this.LogError(data);
                    break;

                case LogLevel.Warning:
                    this.LogWarning(data);
                    break;

                case LogLevel.Debug:
                    this.LogDebug(data);
                    break;

                case LogLevel.Minimal:
                    this.LogMinimal(data);
                    break;

                case LogLevel.Verbose:
                    this.LogVerbose(data);
                    break;

                default:
                case LogLevel.Information:
                    this.LogInformation(data);
                    break;
            }
        }

        public void Log(ILogMessage message)
        {
            this.Log(message.Level, message.FormatWithCode());
        }

        public Task LogAsync(LogLevel level, string data)
        {
            this.Log(level, data);

            return Task.CompletedTask;
        }

        public Task LogAsync(ILogMessage message)
        {
            this.Log(message);

            return Task.CompletedTask;
        }

        public void LogDebug(string data)
        {
            Trace.TraceInformation(data);
        }

        public void LogError(string data)
        {
            Trace.TraceError(data);
        }

        public void LogInformation(string data)
        {
            Trace.TraceInformation(data);
        }

        public void LogInformationSummary(string data)
        {
            Trace.TraceInformation(data);
        }

        public void LogMinimal(string data)
        {
            Trace.TraceInformation(data);
        }

        public void LogVerbose(string data)
        {
            Trace.TraceInformation(data);
        }

        public void LogWarning(string data)
        {
            Trace.TraceWarning(data);
        }
    }
}
