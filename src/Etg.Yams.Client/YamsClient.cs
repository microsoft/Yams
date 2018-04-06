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
            Trace.TraceInformation($"Connecting IPC connections..".FormatMessage(_options));
            if (_initConnection != null)
            {
                await _initConnection.Connect().Timeout(_config.ConnectTimeout, "IPC Monitored Initialization connection failed to connect".FormatMessage(_options));
                Trace.TraceInformation("IPC Monitored Initialization connection connected!".FormatMessage(_options));
            }
            if(_exitConnection != null)
            {
                await _exitConnection.Connect().Timeout(_config.ConnectTimeout, "IPC Graceful Shutdown connection failed to connect".FormatMessage(_options));
                Trace.TraceInformation("IPC Graceful Shutdown connection connected!".FormatMessage(_options));
            }
            if (_healthConnection != null)
            {
                await _healthConnection.Connect().Timeout(_config.ConnectTimeout, "IPC Health connection failed to connect".FormatMessage(_options));
                Trace.TraceInformation("IPC Health connection connected!".FormatMessage(_options));
            }
            _waitForExit = WaitForExit();
        }

        public async Task SendInitializationDoneMessage()
        {
            if (_initConnection == null)
            {
                Trace.TraceError(
                    "Initialization monitoring is not supported for this app. Check your AppConfig.json file".FormatMessage(_options));
                return;
            }
            Trace.TraceInformation("Sending Initialization message to Yams..".FormatMessage(_options));
            await _initConnection.SendMessage("[INITIALIZE_DONE]")
                .Timeout(_config.InitDoneMessageTimeout, "Sending initialization message to Yams has timed out".FormatMessage(_options));
            Trace.TraceInformation("Initialization message has been sent to Yams successfully!".FormatMessage(_options));
        }

        public async Task SendHeartBeat()
        {
            if (_healthConnection == null)
            {
                Trace.TraceError(
                    "Health monitoring is not supported for this app. Check your AppConfig.json file".FormatMessage(_options));
                return;
            }
            Trace.TraceInformation("Sending heart beat message to Yams..".FormatMessage(_options));
            await _healthConnection.SendMessage("[HEALTH_OK]")
                .Timeout(_config.HeartBeatMessageTimeout, "Send heart beat message to Yams has timed out".FormatMessage(_options));
            Trace.TraceInformation("Heart beat message has been sent to Yams successfully!".FormatMessage(_options));
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
                Trace.TraceInformation("Waiting for an exit message from Yams..".FormatMessage(_options));
                string msg = await _exitConnection.ReadMessage();
                if (msg == "[EXIT]")
                {
                    Trace.TraceInformation("Exit request received from Yams".FormatMessage(_options));
                    ExitMessageReceived?.Invoke(this, EventArgs.Empty);
                    break;
                }
                Trace.TraceError($"Unexpected message received from Yams: {msg}, Expected [EXIT]".FormatMessage(_options));
            }
        }
    }
}
