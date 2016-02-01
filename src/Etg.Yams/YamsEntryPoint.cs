using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Watcher;
using Autofac;

namespace Etg.Yams
{
    /// <summary>
    /// The entry point class for YAMS. This class allows one to configure and start YAMS.
    /// </summary>
    public class YamsEntryPoint
    {
        private readonly IDeploymentWatcher _deploymentWatcher;
        private readonly IApplicationPool _applicationPool;
        private static IContainer _container;

        public YamsEntryPoint(YamsConfig config) : this(InitializeDefaultModules(config))
        {
        }

        public YamsEntryPoint(IContainer container)
        {
            _deploymentWatcher = container.Resolve<IDeploymentWatcher>();
            _applicationPool = container.Resolve<IApplicationPool>();
            _container = container;
        }

        public static IContainer InitializeDefaultModules(YamsConfig config)
        {
            var builder = new ContainerBuilder();
            DiModule.RegisterTypes(builder, config);
            return builder.Build();
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
