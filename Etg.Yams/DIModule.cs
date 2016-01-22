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
using Microsoft.Practices.Unity;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams
{
    /// <summary>
    /// Class used for DI registration.
    /// 
    /// Note: Always use InjectionFactory when you resolve a dependency to make sure that resolutions are delayed (lazy). With this approach,
    /// the order of registration/resolution doesn't matter; which makes DI setup a lot easier.
    /// </summary>
    public static class DiModule
    {
        public static void RegisterTypes(IUnityContainer container, YamsConfig config)
        {
            RegisterConfig(container, config);

            RegisterCloudStorageAccount(container);

            RegisterCloudBlobClient(container);

            RegisterBlobLeaseFactory(container);

            RegisterBlobLeaseFactory(container);

            RegisterProcessFactory(container);

            RegisterProcessStopper(container);

            RegisterUpdateSessionManager(container);

            RegisterApplicationConfigSymbolResolver(container);

            RegisterApplicationConfigParser(container);

            RegisterConfigurableApplicationFactory(container);

            RegisterUpdateSessionManagerConfig(container);

            RegisterApplicationDeploymentDirectory(container);

            RegisterApplicationPool(container);

            RegisterApplicationInstaller(container);

            RegisterApplicationDownloader(container);

            RegisterApplicationUpdateManager(container);

            RegisterDeploymentWatcher(container);

			RegisterYamsRepository(container);
        }

        private static void RegisterYamsRepository(IUnityContainer container)
        {
            container.RegisterType<IYamsRepository>(new ContainerControlledLifetimeManager(), new InjectionFactory(
                c =>
                {
                    CloudBlobClient blobClient = c.Resolve<CloudBlobClient>();
                    CloudBlobContainer blobContainer =
                        blobClient.GetContainerReference(Constants.ApplicationsRootFolderName);
                    if (!blobContainer.Exists())
                    {
                        blobContainer.Create();
                    }
                    return new YamsRepository(blobContainer);
                }));
        }

        private static void RegisterUpdateSessionManager(IUnityContainer container)
        {
            container.RegisterType<IUpdateSessionManager, BlobBasedUpdateSessionManager>(
                new ContainerControlledLifetimeManager());
        }

        private static void RegisterDeploymentWatcher(IUnityContainer container)
        {
            container.RegisterType<IDeploymentWatcher>(new ContainerControlledLifetimeManager(), new InjectionFactory(
                c =>
                {
                    var config = c.Resolve<YamsConfig>();
                    return new DeploymentWatcher(c.Resolve<IApplicationUpdateManager>(),
                        config.CheckForUpdatesPeriodInSeconds);
                }));
        }

        private static void RegisterApplicationUpdateManager(IUnityContainer container)
        {
            container.RegisterType<IApplicationUpdateManager>(new ContainerControlledLifetimeManager(), new InjectionFactory(
                c =>
                {
                    var config = c.Resolve<YamsConfig>();
                    return new ApplicationUpdateManager(config.CloudServiceDeploymentId,
                        c.Resolve<IApplicationDeploymentDirectory>(), c.Resolve<IApplicationPool>(),
                        c.Resolve<IApplicationDownloader>(), c.Resolve<IApplicationInstaller>());
                }));
        }

        private static void RegisterConfigurableApplicationFactory(IUnityContainer container)
        {
            container.RegisterType<IApplicationFactory>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(c => new ConfigurableApplicationFactory(
                    c.Resolve<IApplicationConfigParser>(), c.Resolve<IProcessFactory>(), c.Resolve<IProcessStopper>())));
        }

        private static void RegisterProcessStopper(IUnityContainer container)
        {
            container.RegisterType<IProcessStopper>(new ContainerControlledLifetimeManager(), new InjectionFactory(c =>
            {
                var config = c.Resolve<YamsConfig>();
                return new ProcessStopper(config.ProcessWaitForExitInSeconds);
            }));
        }

        private static void RegisterApplicationConfigParser(IUnityContainer container)
        {
            container.RegisterType<IApplicationConfigParser>(new ContainerControlledLifetimeManager(),
                new InjectionFactory(c =>
                {
                    IApplicationConfigSymbolResolver symbolResolver = c.Resolve<IApplicationConfigSymbolResolver>();
                    return new ApplicationConfigParser(symbolResolver);
                }));
        }

        private static void RegisterApplicationConfigSymbolResolver(IUnityContainer container)
        {
            container.RegisterType<IApplicationConfigSymbolResolver>(new ContainerControlledLifetimeManager(),
                new InjectionFactory(c =>
                {
                    YamsConfig config = c.Resolve<YamsConfig>();
                    return new ApplicationConfigSymbolResolver(config.CloudServiceDeploymentId, config.RoleInstanceId);
                }));
        }

        private static void RegisterApplicationPool(IUnityContainer container)
        {
            container.RegisterType<IApplicationPool>(new ContainerControlledLifetimeManager(), 
                new InjectionFactory(c => new ApplicationPool()));
        }

        private static void RegisterApplicationDownloader(IUnityContainer container)
        {
            container.RegisterType<IApplicationDownloader>(new ContainerControlledLifetimeManager(), new InjectionFactory(
                c =>
                {
                    var config = c.Resolve<YamsConfig>();
                    return new ApplicationDownloader(config.ApplicationInstallDirectory, c.Resolve<IYamsRepository>());
                }));
        }

        private static void RegisterApplicationInstaller(IUnityContainer container)
        {
            container.RegisterType<IApplicationInstaller>(new ContainerControlledLifetimeManager(), new InjectionFactory(
                c =>
                {
                    var config = c.Resolve<YamsConfig>();
                    return new ApplicationInstaller(Path.Combine(config.ApplicationInstallDirectory),
                        c.Resolve<IUpdateSessionManager>(), c.Resolve<IApplicationFactory>(), c.Resolve<IApplicationPool>());
                }));
        }

        private static void RegisterProcessFactory(IUnityContainer container)
        {
            container.RegisterType<IProcessFactory>(new ContainerControlledLifetimeManager(),
                new InjectionFactory(c =>
                {
                    var config = container.Resolve<YamsConfig>();
                    return new SelfRestartingProcessFactory(config.ApplicationRestartCount);
                }));
        }

        private static void RegisterBlobLeaseFactory(IUnityContainer container)
        {
            container.RegisterType<IBlobLeaseFactory>(new ContainerControlledLifetimeManager(),
                new InjectionFactory(c =>
                {
                    var config = container.Resolve<YamsConfig>();
                    return new BlobLeaseFactory(config.StorageBlobLeaseRenewIntervalInSeconds);
                }));
        }

        private static void RegisterCloudBlobClient(IUnityContainer container)
        {
            container.RegisterType<CloudBlobClient>(new InjectionFactory(c =>
            {
                var cloudStorageAccount = container.Resolve<CloudStorageAccount>();
                return cloudStorageAccount.CreateCloudBlobClient();
            }));
        }

        private static void RegisterCloudStorageAccount(IUnityContainer container)
        {
            container.RegisterType<CloudStorageAccount>(new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                c =>
                {
                    var config = container.Resolve<YamsConfig>();
                    return CloudStorageAccount.Parse(config.StorageDataConnectionString);
                }));
        }

        private static void RegisterConfig(IUnityContainer container, YamsConfig config)
        {
            container.RegisterInstance(config);
        }

        private static void RegisterUpdateSessionManagerConfig(IUnityContainer container)
        {
            container.RegisterType<UpdateSessionManagerConfig>(new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c =>
                    {
                        var config = c.Resolve<YamsConfig>();
                        return new UpdateSessionManagerConfig(
                            cloudServiceDeploymentId: config.CloudServiceDeploymentId,
                            instanceUpdateDomain: config.InstanceUpdateDomain,
                            instanceId: config.RoleInstanceId,
                            storageContainerName: Constants.ApplicationsRootFolderName);
                    }));
        }

        private static void RegisterApplicationDeploymentDirectory(IUnityContainer container)
        {
            container.RegisterType<IApplicationDeploymentDirectory>(new ContainerControlledLifetimeManager(), new InjectionFactory(
                c => new RemoteApplicationDeploymentDirectory(c.Resolve<IYamsRepository>())));
        }
    }
}
