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
using Etg.Yams.Json;
using Newtonsoft.Json.Serialization;

namespace Etg.Yams
{
    /// <summary>
    /// Class used for DI registration.
    /// </summary>
    public class YamsDiModule
    {
        private readonly IContainer _container;

        public YamsDiModule(YamsConfig config,
            IDeploymentConfigRepository deploymentConfigRepository,
            IApplicationRepository deploymentRepository,
            IDeploymentStatusWriter deploymentStatusWriter,
            IUpdateSessionManager updateSessionManager)
        {
            _container = RegisterTypes(config, deploymentConfigRepository, deploymentRepository, deploymentStatusWriter, 
                updateSessionManager).Build();
        }

        public YamsDiModule(IContainer container)
        {
            _container = container;
        }

        public IYamsService YamsService => _container.Resolve<IYamsService>();

        public IContainer Container => _container;

        public static ContainerBuilder RegisterTypes(YamsConfig config,
            IDeploymentConfigRepository deploymentConfigRepository, IApplicationRepository applicationRepository, IDeploymentStatusWriter deploymentStatusWriter,
            IUpdateSessionManager updateSessionManager)
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

            builder.RegisterInstance(deploymentConfigRepository);
            builder.RegisterInstance(applicationRepository);
            builder.RegisterInstance(new DeploymentRepository(deploymentConfigRepository, applicationRepository)).As<IDeploymentRepository>();
            builder.RegisterInstance(deploymentStatusWriter);

            builder.RegisterType<YamsService>().As<IYamsService>().SingleInstance();

            RegisterAppDeploymentMatcher(builder);

            builder.RegisterType<DiagnosticsTraceWriter>().As<ITraceWriter>().SingleInstance();
            builder.RegisterType<JsonSerializer>().As<IJsonSerializer>().SingleInstance();


            return builder;
        }

        private static void RegisterAppDeploymentMatcher(ContainerBuilder builder)
        {
            builder.Register<IAppDeploymentMatcher>(c =>
            {
                var config = c.Resolve<YamsConfig>();
                var propertiesDeploymentMatcher = new PropertiesDeploymentMatcher(config.ClusterProperties);
                var clusterIdDeploymentMatcher = new ClusterIdDeploymentMatcher(config.ClusterId);
                return new AndDeploymentMatcher(clusterIdDeploymentMatcher, propertiesDeploymentMatcher);
            });
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
                    return new ApplicationUpdateManager(config.ClusterId, config.InstanceId,
                        c.Resolve<IApplicationDeploymentDirectory>(), c.Resolve<IApplicationPool>(),
                        c.Resolve<IApplicationDownloader>(), c.Resolve<IApplicationInstaller>(),
                        c.Resolve<IDeploymentStatusWriter>(), c.Resolve<IUpdateSessionManager>());
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
            builder.RegisterType<ApplicationConfigParser>().As<IApplicationConfigParser>().SingleInstance();
        }

        private static void RegisterApplicationConfigSymbolResolver(ContainerBuilder builder)
        {
            builder.Register<IApplicationConfigSymbolResolver>(c =>
                {
                    YamsConfig config = c.Resolve<YamsConfig>();
                    return new ApplicationConfigSymbolResolver(config.ClusterId, config.InstanceId, config.ClusterProperties);
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
                        c.Resolve<IApplicationFactory>(), c.Resolve<IApplicationPool>());
                }).SingleInstance();
        }

        private static void RegisterProcessFactory(ContainerBuilder builder)
        {
            builder.Register<IProcessFactory>(c =>
                {
                    var config = c.Resolve<YamsConfig>();
                    return new ProcessFactory(config);
                }).SingleInstance();
        }

        private static void RegisterConfig(ContainerBuilder builder, YamsConfig config)
        {
            builder.RegisterInstance(config);
        }

        private static void RegisterApplicationDeploymentDirectory(ContainerBuilder builder)
        {
            builder.Register<IApplicationDeploymentDirectory>(
                c => new RemoteApplicationDeploymentDirectory(c.Resolve<IDeploymentRepository>(),
                c.Resolve<IAppDeploymentMatcher>())).SingleInstance();
        }
    }
}
