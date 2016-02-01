using System.IO;
using Etg.Yams.Application;
using Etg.Yams.Deploy;
using Etg.Yams.Download;
using Etg.Yams.Process;
using Etg.Yams.Install;
using Etg.Yams.Lease;
using Etg.Yams.Storage;
using Etg.Yams.Update;
using Etg.Yams.Watcher;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Autofac;

namespace Etg.Yams
{
    /// <summary>
    /// Class used for DI registration.
    /// </summary>
    public static class DiModule
    {
        public static void RegisterTypes(ContainerBuilder builder, YamsConfig config)
        {
            RegisterConfig(builder, config);

            RegisterCloudStorageAccount(builder);

            RegisterCloudBlobClient(builder);

            RegisterBlobLeaseFactory(builder);

            RegisterBlobLeaseFactory(builder);

            RegisterProcessFactory(builder);

            RegisterProcessStopper(builder);

            RegisterUpdateSessionManager(builder);

            RegisterApplicationConfigSymbolResolver(builder);

            RegisterApplicationConfigParser(builder);

            RegisterConfigurableApplicationFactory(builder);

            RegisterUpdateSessionManagerConfig(builder);

            RegisterApplicationDeploymentDirectory(builder);

            RegisterApplicationPool(builder);

            RegisterApplicationInstaller(builder);

            RegisterApplicationDownloader(builder);

            RegisterApplicationUpdateManager(builder);

            RegisterDeploymentWatcher(builder);

            RegisterDeploymentRepository(builder);
        }

        private static void RegisterDeploymentRepository(ContainerBuilder builder)
        {
            builder.Register<IDeploymentRepository>(c =>
            {
                CloudBlobClient blobClient = c.Resolve<CloudBlobClient>();
                CloudBlobContainer blobContainer =
                    blobClient.GetContainerReference(Constants.ApplicationsRootFolderName);
                if (!blobContainer.Exists())
                {
                    blobContainer.Create();
                }
                return new BlobStorageDeploymentRepository(blobContainer);
            }).SingleInstance();
        }

        private static void RegisterUpdateSessionManager(ContainerBuilder builder)
        {
            builder.RegisterType<BlobBasedUpdateSessionManager>().As<IUpdateSessionManager>().SingleInstance();
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
                    return new ApplicationUpdateManager(config.CloudServiceDeploymentId,
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
                    return new ApplicationConfigSymbolResolver(config.CloudServiceDeploymentId, config.RoleInstanceId);
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
                    return new SelfRestartingProcessFactory(config.ApplicationRestartCount);
                }).SingleInstance();
        }

        private static void RegisterBlobLeaseFactory(ContainerBuilder builder)
        {
            builder.Register<IBlobLeaseFactory>(c =>
                {
                    var config = c.Resolve<YamsConfig>();
                    return new BlobLeaseFactory(config.StorageBlobLeaseRenewIntervalInSeconds);
                }).SingleInstance();
        }

        private static void RegisterCloudBlobClient(ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var cloudStorageAccount = c.Resolve<CloudStorageAccount>();
                return cloudStorageAccount.CreateCloudBlobClient();
            });
        }

        private static void RegisterCloudStorageAccount(ContainerBuilder builder)
        {
            builder.Register(
                c =>
                {
                    var config = c.Resolve<YamsConfig>();
                    return CloudStorageAccount.Parse(config.StorageDataConnectionString);
                }).SingleInstance();
        }

        private static void RegisterConfig(ContainerBuilder builder, YamsConfig config)
        {
            builder.RegisterInstance(config);
        }

        private static void RegisterUpdateSessionManagerConfig(ContainerBuilder builder)
        {
            builder.Register(
                    c =>
                    {
                        var config = c.Resolve<YamsConfig>();
                        return new UpdateSessionManagerConfig(
                            cloudServiceDeploymentId: config.CloudServiceDeploymentId,
                            instanceUpdateDomain: config.InstanceUpdateDomain,
                            instanceId: config.RoleInstanceId,
                            storageContainerName: Constants.ApplicationsRootFolderName);
                    }).SingleInstance();
        }

        private static void RegisterApplicationDeploymentDirectory(ContainerBuilder builder)
        {
            builder.Register<IApplicationDeploymentDirectory>(
                c => new RemoteApplicationDeploymentDirectory(c.Resolve<IDeploymentRepository>())).SingleInstance();
        }
    }
}
