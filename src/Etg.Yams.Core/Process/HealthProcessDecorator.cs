using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Ipc;
using Etg.Yams.Utils;

namespace Etg.Yams.Process
{
    public class HealthProcessDecorator : AbstractProcessDecorator
    {
        private readonly YamsConfig _config;
        private readonly IIpcConnection _ipcConnection;
        private CancellationTokenSource _watchProcessHealthCancellationTokenSource;
        private Task _watchProcessHealthTask;

        public HealthProcessDecorator(AppIdentity identity, YamsConfig config, IProcess process,
            IIpcConnection ipcConnection) : base(identity, process)
        {
            _config = config;
            _ipcConnection = ipcConnection;
        }

        public override async Task Start(string args)
        {
            await _process.Start($"{args} --HealthPipeName {_ipcConnection.ConnectionId}");

            await _ipcConnection.Connect().Timeout(_config.IpcConnectTimeout,
                $"Connecting to health pipe has timed out, make sure that the app {this.Identity} is connecting to the same pipe");

            MonitorProcessHealth();
        }

        public override async Task Close()
        {
            await StopMonitoringProcessHealth();
            await base.Close();
        }

        public override async Task Kill()
        {
            await StopMonitoringProcessHealth();
            await base.Kill();
        }

        private void MonitorProcessHealth()
        {
            _watchProcessHealthCancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _watchProcessHealthCancellationTokenSource.Token;
            _watchProcessHealthTask = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Trace.TraceInformation($"Waiting for heart beat from app {this.Identity}");
                    Task<string> readMessageTask = _ipcConnection.ReadMessage();

                    await WaitForHeartBeat(cancellationToken, readMessageTask);
                }
            }, cancellationToken);
        }

        private async Task WaitForHeartBeat(CancellationToken cancellationToken, Task<string> readMessageTask)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var completedTask = await Task.WhenAny(readMessageTask,
                    Task.Delay(_config.AppHeartBeatTimeout, cancellationToken));

                if (completedTask == readMessageTask)
                {
                    string msg = readMessageTask.Result;
                    if (msg == "[HEALTH_OK]")
                    {
                        Trace.TraceInformation($"Heart beat received from App {this.Identity}; App is healthy");
                        break;
                    }
                    Trace.TraceError(
                        $"Unexpected message '{msg}' received from App {this.Identity} instead of heart beat");
                }
                else if(!cancellationToken.IsCancellationRequested)
                {
                    Trace.TraceError($"Heart beat has not been received in time from {this.Identity}; App is unhealthy");
                }
            }
        }

        private async Task StopMonitoringProcessHealth()
        {
            if (_watchProcessHealthCancellationTokenSource != null && !_watchProcessHealthCancellationTokenSource.IsCancellationRequested)
            {
                _watchProcessHealthCancellationTokenSource.Cancel();
            }
            if (_watchProcessHealthTask != null)
            {
                await _watchProcessHealthTask;
            }

            _watchProcessHealthCancellationTokenSource?.Dispose();
            _watchProcessHealthCancellationTokenSource = null;
        }
    }
}