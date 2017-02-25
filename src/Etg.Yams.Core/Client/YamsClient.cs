using Etg.Yams.Process.Ipc;
using Etg.Yams.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Etg.Yams.Client
{
    public class YamsClient : IYamsClient
    {
        private readonly YamsClientConfig _config;
        private readonly IIpcConnection _initConnection;
        private readonly IIpcConnection _exitConnection;
        private readonly IIpcConnection _healthConnection;
        private Task _waitForExit;

        public event EventHandler ExitMessageReceived;

        public YamsClient(YamsClientConfig config, IIpcConnection initConnection, IIpcConnection exitConnection,
            IIpcConnection healthConnection)
        {
            _config = config;
            _initConnection = initConnection;
            _exitConnection = exitConnection;
            _healthConnection = healthConnection;
        }

        public async Task Connect()
        {
            Trace.TraceInformation($"Connecting IPC connections..");
            if (_initConnection != null)
            {
                await _initConnection.Connect().Timeout(_config.ConnectTimeout, "IPC connection failed to connect");
            }
            if(_exitConnection != null)
            {
                await _exitConnection.Connect().Timeout(_config.ConnectTimeout, "IPC connection failed to connect");
            }
            if (_healthConnection != null)
            {
                await _healthConnection.Connect().Timeout(_config.ConnectTimeout, "IPC connection failed to connect");
            }
            Trace.TraceInformation("IPC connections connected!");
            _waitForExit = WaitForExit();
        }

        public async Task SendInitializationDoneMessage()
        {
            if (_initConnection == null)
            {
                Trace.TraceError(
                    "Initialization monitoring is not supported for this app. Check your AppConfig.json file");
                return;
            }
            Trace.TraceInformation("Sending Initialization message to Yams..");
            await _initConnection.SendMessage("[INITIALIZE_DONE]")
                .Timeout(_config.InitDoneMessageTimeout, "Sending initialization message to Yams has timed out");
            Trace.TraceInformation("Initialization message has been sent to Yams successfully!");
        }

        public async Task SendHeartBeat()
        {
            if (_healthConnection == null)
            {
                Trace.TraceError(
                    "Health monitoring is not supported for this app. Check your AppConfig.json file");
                return;
            }
            Trace.TraceInformation("Sending heart beat message to Yams..");
            await _healthConnection.SendMessage("[HEALTH_OK]")
                .Timeout(_config.HeartBeatMessageTimeout, "Send heart beat message to Yams has timed out");
            Trace.TraceInformation("Heart beat message has been sent to Yams successfully!");
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
                Trace.TraceInformation("Waiting for an exit message from Yams..");
                string msg = await _exitConnection.ReadMessage();
                if (msg == "[EXIT]")
                {
                    Trace.TraceInformation("Exit request received from Yams");
                    ExitMessageReceived?.Invoke(this, EventArgs.Empty);
                    break;
                }
                Trace.TraceError($"Unexpected message received from app: {msg}, Expected [EXIT]");
            }
        }
    }
}
