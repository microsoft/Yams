using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Etg.Yams.Utils;
using Etg.Yams.Ipc;
using Etg.Yams.Application;

namespace Etg.Yams.Process
{
    public class MonitorInitProcessDecorator : AbstractProcessDecorator
    {
        private readonly YamsConfig _config;
        private readonly IIpcConnection _ipcConnection;

        public MonitorInitProcessDecorator(AppIdentity identity, YamsConfig config, IProcess process,
            IIpcConnection ipcConnection) : base(identity, process)
        {
            _config = config;
            _ipcConnection = ipcConnection;
        }

        public override void Dispose()
        {
            base.Dispose();
            _ipcConnection?.Dispose();
        }

        public override async Task Start(string args)
        {
            await _process.Start($"{args} --InitializationPipeName {_ipcConnection.ConnectionId}");

            try
            {
                await _ipcConnection.Connect().Timeout(_config.IpcConnectTimeout,
                    $"Connecting to initialization pipe has timed out, make sure that the app {this.Identity} is connecting to the same pipe");

                Trace.TraceInformation($"Yams is waiting for the app {this.Identity} to finish initializing");
                string msg = await _ipcConnection.ReadMessage()
                    .Timeout(_config.AppInitTimeout, $"Did not receive initialized message from the app {ExePath}");

                if (msg != "[INITIALIZE_DONE]")
                {
                    throw new InvalidOperationException($"Unexpected message '{msg}' received from app {this.Identity}");
                }
            }
            catch (Exception)
            {
                await Kill();
                throw;
            }

            Trace.TraceInformation($"Received initialized message from App {this.Identity}; App is ready to receive requests");
        }

        public override async Task Kill()
        {
            await Task.WhenAll(_ipcConnection.Disconnect(), base.Kill());
        }
    }
}