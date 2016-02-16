using System.IO;
using Etg.Yams.Application;
using Etg.Yams.Deploy;
using Etg.Yams.Download;
using Etg.Yams.Process;
using Etg.Yams.Install;
using Etg.Yams.Storage;
using Etg.Yams.Update;
using Etg.Yams.Watcher;
using Autofac;

namespace Etg.Yams
{
    /// <summary>
    /// Class used for DI registration.
    /// </summary>
    public class YamsDiModule
    {
        private readonly IContainer _container;

        public YamsDiModule(YamsConfig config, IDeploymentRepository deploymentRepository, 
            IUpdateSessionManager updateSessionManager)
        {
            _container = RegisterTypes(config, deploymentRepository, updateSessionManager).Build();
        }

        public YamsDiModule(IContainer container)
        {
            _container = container;
        }

        public IYamsService YamsService => _container.Resolve<IYamsService>();

        public IContainer Container => _container;

        public static ContainerBuilder RegisterTypes(YamsConfig config, 
            IDeploymentRepository deploymentRepository, IUpdateSessionManager updateSessionManager)
        {
            var builder = new ContainerBuilder();

            RegisterConfig(builder, config);

            RegisterProcessFactory(builder);

            RegisterProcessStopper(builder);

            RegisterApplicationConfigSymbolResolver(builder);

            RegisterApplicationConfigParser(builder);

            RegisterConfigurableApplicationFactory(builder);

            RegisterApplicationDeploymentDirectory(builder);

            RegisterApplicationPool(builder);

            RegisterApplicationInstaller(builder);

            RegisterApplicationDownloader(builder);

            RegisterApplicationUpdateManager(builder);

            RegisterDeploymentWatcher(builder);

            builder.RegisterInstance(updateSessionManager);

            builder.RegisterInstance(deploymentRepository);

            builder.RegisterType<YamsService>().As<IYamsService>().SingleInstance();

            return builder;
        }

        private static void RegisterDeploymentWatcher(ContainerBuilder builder)
        {
            builder.Register<IDeploymentWatcher>(c =>
            {
                var config = c.Resolve<YamsConfig>();
                return new DeploymentWatcher(c.Resolve<IApplicationUpdateManager>(),
                    config.CheckForUpdatesPeriodInSeconds);
            }).SingleInstance();
        }

        private static void RegisterApplicationUpdateManager(ContainerBuilder builder)
        {
            builder.Register<IApplicationUpdateManager>(
                c =>
                {
                    var config = c.Resolve<YamsConfig>();
                    return new ApplicationUpdateManager(config.ClusterDeploymentId,
                        c.Resolve<IApplicationDeploymentDirectory>(), c.Resolve<IApplicationPool>(),
                        c.Resolve<IApplicationDownloader>(), c.Resolve<IApplicationInstaller>());
                }).SingleInstance();
        }

        private static void RegisterConfigurableApplicationFactory(ContainerBuilder builder)
        {
            builder.Register<IApplicationFactory>(c => new ConfigurableApplicationFactory(
                    c.Resolve<IApplicationConfigParser>(), c.Resolve<IProcessFactory>(), c.Resolve<IProcessStopper>())).SingleInstance();
        }

        private static void RegisterProcessStopper(ContainerBuilder builder)
        {
            builder.Register<IProcessStopper>(c =>
            {
                var config = c.Resolve<YamsConfig>();
                return new ProcessStopper(config.ProcessWaitForExitInSeconds);
            }).SingleInstance();
        }

        private static void RegisterApplicationConfigParser(ContainerBuilder builder)
        {
            builder.Register<IApplicationConfigParser>(c =>
                {
                    IApplicationConfigSymbolResolver symbolResolver = c.Resolve<IApplicationConfigSymbolResolver>();
                    return new ApplicationConfigParser(symbolResolver);
                }).SingleInstance();
        }

        private static void RegisterApplicationConfigSymbolResolver(ContainerBuilder builder)
        {
            builder.Register<IApplicationConfigSymbolResolver>(c =>
                {
                    YamsConfig config = c.Resolve<YamsConfig>();
                    return new ApplicationConfigSymbolResolver(config.ClusterDeploymentId, config.InstanceId);
                }).SingleInstance();
        }

        private static void RegisterApplicationPool(ContainerBuilder builder)
        {
            builder.Register<IApplicationPool>(c => new ApplicationPool()).SingleInstance();
        }

        private static void RegisterApplicationDownloader(ContainerBuilder builder)
        {
            builder.Register<IApplicationDownloader>(
                c =>
                {
                    var config = c.Resolve<YamsConfig>();
                    return new ApplicationDownloader(config.ApplicationInstallDirectory, c.Resolve<IDeploymentRepository>());
                }).SingleInstance();
        }

        private static void RegisterApplicationInstaller(ContainerBuilder builder)
        {
            builder.Register<IApplicationInstaller>(
                c =>
                {
                    var config = c.Resolve<YamsConfig>();
                    return new ApplicationInstaller(Path.Combine(config.ApplicationInstallDirectory),
                        c.Resolve<IUpdateSessionManager>(), c.Resolve<IApplicationFactory>(), c.Resolve<IApplicationPool>());
                }).SingleInstance();
        }

        private static void RegisterProcessFactory(ContainerBuilder builder)
        {
            builder.Register<IProcessFactory>(c =>
                {
                    var config = c.Resolve<YamsConfig>();
                    return new SelfRestartingProcessFactory(config.ApplicationRestartCount, config.ShowApplicationProcessWindow);
                }).SingleInstance();
        }

        private static void RegisterConfig(ContainerBuilder builder, YamsConfig config)
        {
            builder.RegisterInstance(config);
        }

        private static void RegisterApplicationDeploymentDirectory(ContainerBuilder builder)
        {
            builder.Register<IApplicationDeploymentDirectory>(
                c => new RemoteApplicationDeploymentDirectory(c.Resolve<IDeploymentRepository>())).SingleInstance();
        }
    }
}
