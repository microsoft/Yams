using Etg.Yams.Application;
using Etg.Yams.Ipc;
using Etg.Yams.Os;
using System;

namespace Etg.Yams.Process
{
    public class ProcessFactory : IProcessFactory
    {
        private readonly YamsConfig _config;
        private readonly ISystem system;

        public ProcessFactory(YamsConfig config, ISystem system)
        {
            _config = config;
            this.system = system;
        }

        public IProcess CreateProcess(AppIdentity identity, string exePath, bool monitorInitialization, bool monitorHealth,
            bool gracefulShutdown)
        {
            IProcess process = new Process(identity, exePath, _config.UseShellExecute, system);

            if (monitorInitialization)
            {
                string pipeName = Guid.NewGuid().ToString();
                process = new MonitorInitProcessDecorator(identity, _config, process,
                    new IpcConnection(new NamedPipeServerAdapter(pipeName)));
            }
            if (gracefulShutdown)
            {
                string pipeName = Guid.NewGuid().ToString();
                process = new GracefulShutdownProcessDecorator(identity, _config, process,
                    new IpcConnection(new NamedPipeServerAdapter(pipeName)));
            }
            if (monitorHealth)
            {
                string pipeName = Guid.NewGuid().ToString();
                process = new HealthProcessDecorator(identity, _config, process,
                    new IpcConnection(new NamedPipeServerAdapter(pipeName)));
            }
            return new SelfRestartingProcess(process, _config.ApplicationRestartCount);
        }
    }
}