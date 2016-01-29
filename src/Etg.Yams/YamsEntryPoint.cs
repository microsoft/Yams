using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Watcher;
using Microsoft.Practices.Unity;

namespace Etg.Yams
{
    /// <summary>
    /// The entry point class for YAMS. This class allows one to configure and start YAMS.
    /// </summary>
    public class YamsEntryPoint
    {
        private readonly IDeploymentWatcher _deploymentWatcher;
        private readonly IApplicationPool _applicationPool;
        private static IUnityContainer _unityContainer;

        public YamsEntryPoint(YamsConfig config) : this(InitializeDefaultUnityContainer(config))
        {
        }

        public YamsEntryPoint(IUnityContainer unityContainer)
        {
            _deploymentWatcher = unityContainer.Resolve<IDeploymentWatcher>();
            _applicationPool = unityContainer.Resolve<IApplicationPool>();
            _unityContainer = unityContainer;
        }

        public static IUnityContainer InitializeDefaultUnityContainer(YamsConfig config)
        {
            _unityContainer = new UnityContainer();
            DiModule.RegisterTypes(_unityContainer, config);
            return _unityContainer;
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
    }
}
