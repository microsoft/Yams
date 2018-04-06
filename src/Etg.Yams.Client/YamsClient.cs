using Etg.Yams.Ipc;
using Etg.Yams.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Etg.Yams.Client
{
    public class YamsClient : IYamsClient
    {
        private readonly YamsClientOptions _options;
        private readonly YamsClientConfig _config;
        private readonly IIpcConnection _initConnection;
        private readonly IIpcConnection _exitConnection;
        private readonly IIpcConnection _healthConnection;
        private Task _waitForExit;

        public event EventHandler ExitMessageReceived;

        internal YamsClient(YamsClientOptions options, YamsClientConfig config, IIpcConnection initConnection, IIpcConnection exitConnection,
            IIpcConnection healthConnection)
        {
            _options = options;
            _config = config;
            _initConnection = initConnection;
            _exitConnection = exitConnection;
            _healthConnection = healthConnection;
        }

        public async Task Connect()
        {
            Trace.TraceInformation(FormatMessage($"Connecting IPC connections..", _options));
            if (_initConnection != null)
            {
                await _initConnection.Connect().Timeout(_config.ConnectTimeout, FormatMessage("IPC Monitored Initialization connection failed to connect", _options));
                Trace.TraceInformation(FormatMessage("IPC Monitored Initialization connection connected!", _options));
            }
            if(_exitConnection != null)
            {
                await _exitConnection.Connect().Timeout(_config.ConnectTimeout, FormatMessage("IPC Graceful Shutdown connection failed to connect", _options));
                Trace.TraceInformation(FormatMessage("IPC Graceful Shutdown connection connected!", _options));
            }
            if (_healthConnection != null)
            {
                await _healthConnection.Connect().Timeout(_config.ConnectTimeout, FormatMessage("IPC Health connection failed to connect", _options));
                Trace.TraceInformation(FormatMessage("IPC Health connection connected!", _options));
            }
            _waitForExit = WaitForExit();
        }

        public async Task SendInitializationDoneMessage()
        {
            if (_initConnection == null)
            {
                Trace.TraceError(
                    FormatMessage("Initialization monitoring is not supported for this app. Check your AppConfig.json file", _options));
                return;
            }
            Trace.TraceInformation(FormatMessage("Sending Initialization message to Yams..", _options));
            await _initConnection.SendMessage("[INITIALIZE_DONE]")
                .Timeout(_config.InitDoneMessageTimeout, FormatMessage("Sending initialization message to Yams has timed out", _options));
            Trace.TraceInformation(FormatMessage("Initialization message has been sent to Yams successfully!", _options));
        }

        public async Task SendHeartBeat()
        {
            if (_healthConnection == null)
            {
                Trace.TraceError(
                    FormatMessage("Health monitoring is not supported for this app. Check your AppConfig.json file", _options));
                return;
            }
            Trace.TraceInformation(FormatMessage("Sending heart beat message to Yams..", _options));
            await _healthConnection.SendMessage("[HEALTH_OK]")
                .Timeout(_config.HeartBeatMessageTimeout, FormatMessage("Send heart beat message to Yams has timed out", _options));
            Trace.TraceInformation(FormatMessage("Heart beat message has been sent to Yams successfully!", _options));
        }

        public void Dispose()
        {
            _initConnection?.Dispose();
            _exitConnection?.Dispose();
            _healthConnection?.Dispose();
        }

        private async Task WaitForExit()
        {
            if(_exitConnection == null)
            {
                return;
            }
            while (true)
            {
                Trace.TraceInformation(FormatMessage("Waiting for an exit message from Yams..", _options));
                string msg = await _exitConnection.ReadMessage();
                if (msg == "[EXIT]")
                {
                    Trace.TraceInformation(FormatMessage("Exit request received from Yams", _options));
                    ExitMessageReceived?.Invoke(this, EventArgs.Empty);
                    break;
                }
                Trace.TraceError(FormatMessage($"Unexpected message received from Yams: {msg}, Expected [EXIT]", _options));
            }
        }

        private static string FormatMessage(string message, YamsClientOptions options)
        {
            return $"[{options.AppName} ({options.AppVersion})] {message}";
        }
    }
}
