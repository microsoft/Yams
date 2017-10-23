using Etg.Yams.Ipc;
using System;

namespace Etg.Yams.Process
{
    public class ProcessFactory : IProcessFactory
    {
        private readonly YamsConfig _config;

        public ProcessFactory(YamsConfig config)
        {
            _config = config;
        }

        public IProcess CreateProcess(string exePath, bool monitorInitialization, bool monitorHealth,
            bool gracefulShutdown)
        {
            IProcess process = new Process(exePath, _config.ShowApplicationProcessWindow);

            if (gracefulShutdown)
            {
                string pipeName = Guid.NewGuid().ToString();
                process = new GracefulShutdownProcessDecorator(_config, process,
                    new IpcConnection(new NamedPipeServerAdapter(pipeName)));
            }
            if (monitorHealth)
            {
                string pipeName = Guid.NewGuid().ToString();
                process = new HealthProcessDecorator(_config, process,
                    new IpcConnection(new NamedPipeServerAdapter(pipeName)));
            }
            if (monitorInitialization)
            {
                string pipeName = Guid.NewGuid().ToString();
                process = new MonitorInitProcessDecorator(_config, process,
                    new IpcConnection(new NamedPipeServerAdapter(pipeName)));
            }
            return new SelfRestartingProcess(process, _config.ApplicationRestartCount);
        }
    }
}