using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Watcher;
using Autofac;
using System;

namespace Etg.Yams
{
    /// <summary>
    /// The entry point class for YAMS. This class allows one to configure and start YAMS.
    /// </summary>
    public class YamsService : IYamsService, IDisposable
    {
        private readonly IDeploymentWatcher _deploymentWatcher;
        private readonly IApplicationPool _applicationPool;

        public YamsService(IDeploymentWatcher deploymentWatcher, IApplicationPool applicationPool) 
        {
            _deploymentWatcher = deploymentWatcher;
            _applicationPool = applicationPool;
        }

        public Task Start()
        {
            return _deploymentWatcher.Start();
        }

        public async Task Stop()
        {
            await _deploymentWatcher.Stop();
            await _applicationPool.Shutdown();
        }

        public void Dispose()
        {
            Stop().Wait();
            _applicationPool.Dispose();
        }
    }
}
