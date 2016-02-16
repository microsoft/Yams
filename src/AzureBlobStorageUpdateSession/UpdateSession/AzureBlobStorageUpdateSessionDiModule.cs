using System;
using Autofac;
using Etg.Yams.Azure.Lease;
using Etg.Yams.Azure.UpdateSession.Retry;
using Etg.Yams.Azure.Utils;
using Etg.Yams.Update;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Etg.Yams.Azure.UpdateSession
{
    public class AzureBlobStorageUpdateSessionDiModule
    {
        private readonly IContainer _container;
        private const string UpdateBlobFactoryRetryStrategyModuleName = "UpdateBlobFactoryRetryStrategy";
        private const string UpdateSessionRetryStrategyModuleName = "updateSessionRetryStrategy";

        public AzureBlobStorageUpdateSessionDiModule(
            string deploymentId,
            string instanceId,
            string updateDomain,
            string connectionString,
            string blobContainerName = "applications",
            int lockBlobRetryCount = 20,
            int lockBlobRetryIntervalInSeconds = 1,
            int storageExceptionRetryCount = 20,
            int storageExceptionRetryIntervalInSeconds = 1) : this(RegisterTypes(
                deploymentId, instanceId, updateDomain, connectionString, blobContainerName, lockBlobRetryCount,
                lockBlobRetryIntervalInSeconds, storageExceptionRetryCount, storageExceptionRetryIntervalInSeconds).Build())
        {
        }

        public AzureBlobStorageUpdateSessionDiModule(IContainer container)
        {
            _container = container;
        }

        public static ContainerBuilder RegisterTypes(string deploymentId,
            string instanceId,
            string updateDomain,
            string connectionString,
            string blobContainerName = "applications",
            int lockBlobRetryCount = 20,
            int lockBlobRetryIntervalInSeconds = 1,
            int storageExceptionRetryCount = 20,
            int storageExceptionRetryIntervalInSeconds = 1)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<BlobLeaseFactory>().As<IBlobLeaseFactory>().SingleInstance();

            containerBuilder.Register<IUpdateBlobFactory>(c =>
                new UpdateBlobFactoryRetryLockDecorator(
                    new UpdateBlobFactory(deploymentId,
                        BlobUtils.GetBlobContainer(connectionString, blobContainerName),
                        c.Resolve<IBlobLeaseFactory>()),
                    c.ResolveNamed<RetryStrategy>(UpdateBlobFactoryRetryStrategyModuleName)
                    )).SingleInstance();

            containerBuilder.Register<RetryStrategy>(
                c => new FixedInterval(lockBlobRetryCount, TimeSpan.FromSeconds(lockBlobRetryIntervalInSeconds)))
                .Named<RetryStrategy>(UpdateBlobFactoryRetryStrategyModuleName).SingleInstance();

            containerBuilder.Register<RetryStrategy>(
                c => new FixedInterval(storageExceptionRetryCount, TimeSpan.FromSeconds(storageExceptionRetryIntervalInSeconds)))
                .Named<RetryStrategy>(UpdateSessionRetryStrategyModuleName).SingleInstance();

            containerBuilder.Register(
                c => new BlobBasedUpdateSessionManager(c.Resolve<IUpdateBlobFactory>(), instanceId, updateDomain));

            containerBuilder.Register<IUpdateSessionManager>(
                c => new UpdateSessionManagerRetryDecorator(
                    c.Resolve<BlobBasedUpdateSessionManager>(),
                    c.ResolveNamed<RetryStrategy>(UpdateSessionRetryStrategyModuleName),
                    new StorageExceptionErrorDetectionStrategy()));
            return containerBuilder;
        }

        public IContainer Container => _container;

        public IUpdateSessionManager UpdateSessionManager => _container.Resolve<IUpdateSessionManager>();
    }
}
