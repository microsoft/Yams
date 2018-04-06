using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Etg.Yams.Utils;
using Etg.Yams.Ipc;
using Etg.Yams.Application;

namespace Etg.Yams.Process
{
    public class GracefulShutdownProcessDecorator : AbstractProcessDecorator
    {
        private readonly YamsConfig _config;
        private readonly IIpcConnection _ipcConnection;

        public GracefulShutdownProcessDecorator(AppIdentity identity, YamsConfig config, IProcess process, IIpcConnection ipcConnection)
            : base(identity, process)
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
            var startProcessTask =
                _process.Start($"{args} --ExitPipeName {_ipcConnection.ConnectionId}");
            await Task.WhenAll(startProcessTask, _ipcConnection.Connect().Timeout(_config.IpcConnectTimeout,
                $"Connecting to graceful exit pipe has timed out, make sure that the app {this.Identity} is connecting to the same pipe"));
        }

        public override async Task Close()
        {
            Trace.TraceInformation($"Yams is sending an exit message to the app {this.Identity}");
            try
            {
                UnsubscribeFromExited();

                await _ipcConnection.SendMessage("[EXIT]").Timeout(_config.GracefulShutdownMessageTimeout);

                Trace.TraceInformation($"Exit message sent to {this.Identity} !");

                await _ipcConnection.Disconnect();

                if (!await ProcessUtils.SpinWaitForExit(this, (int) _config.AppGracefulShutdownTimeout.TotalSeconds))
                {
                    throw new TimeoutException($"The app {this.Identity} did not exit in time");
                }
                Trace.TraceInformation($"App {this.Identity} has exited gracefully!");
            }
            catch (TimeoutException e)
            {
                Trace.TraceError($"App {this.Identity} did not respond to exit message, attempting to close the process.. {e.Message}");
                await _process.Close();
                Trace.TraceInformation($"App {this.Identity} has been closed");
            }
        }
    }
}