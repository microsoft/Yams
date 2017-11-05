using System;
using Autofac;
using Etg.Yams.Azure.Lease;
using Etg.Yams.Azure.UpdateSession.Retry;
using Etg.Yams.Update;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Etg.Yams.Azure.UpdateSession
{
    public class AzureStorageUpdateSessionDiModule
    {
        private readonly IContainer _container;
        private const string UpdateSessionRetryStrategyModuleName = "updateSessionRetryStrategy";

        public AzureStorageUpdateSessionDiModule(
            string clusterId,
            string instanceId,
            string updateDomain,
            string connectionString,
            TimeSpan updateSessionTtl,
            int storageExceptionRetryCount = 20,
            int storageExceptionRetryIntervalInSeconds = 1,
            int startUpdateSessionRetryCount = 5,
            int startUpdateSessionRetryIntervalInSeconds = 1) : this(RegisterTypes(
                clusterId, instanceId, updateDomain, connectionString, updateSessionTtl,
                storageExceptionRetryCount, storageExceptionRetryIntervalInSeconds,
                startUpdateSessionRetryCount, startUpdateSessionRetryIntervalInSeconds).Build())
        {
        }

        public AzureStorageUpdateSessionDiModule(IContainer container)
        {
            _container = container;
        }

        public static ContainerBuilder RegisterTypes(string clusterId,
            string instanceId,
            string updateDomain,
            string connectionString,
            TimeSpan updateSessionTtl,
            int storageExceptionRetryCount = 20,
            int storageExceptionRetryIntervalInSeconds = 1,
            int startUpdateSessionRetryCount = 5,
            int startUpdateSessionRetryIntervalInSeconds = 1)
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder.Register<RetryStrategy>(
                c => new FixedInterval(storageExceptionRetryCount, TimeSpan.FromSeconds(storageExceptionRetryIntervalInSeconds)))
                .Named<RetryStrategy>(UpdateSessionRetryStrategyModuleName).SingleInstance();

            containerBuilder.Register(
                c => new AzureTableUpdateSessionManager(c.Resolve<IUpdateSessionTable>(), clusterId, instanceId,
                    updateDomain));

            containerBuilder.RegisterInstance(new UpdateSessionTable(connectionString, updateSessionTtl))
                .As<IUpdateSessionTable>();
            containerBuilder.Register<IUpdateSessionManager>(
                c => 
                new StartUpdateSessionRetryDecorator(
                new StorageExceptionUpdateSessionRetryDecorator(
                    c.Resolve<AzureTableUpdateSessionManager>(),
                    c.ResolveNamed<RetryStrategy>(UpdateSessionRetryStrategyModuleName),
                    new StorageExceptionErrorDetectionStrategy()), startUpdateSessionRetryCount, 
                TimeSpan.FromSeconds(startUpdateSessionRetryIntervalInSeconds)));
            return containerBuilder;
        }

        public IContainer Container => _container;

        public IUpdateSessionManager UpdateSessionManager => _container.Resolve<IUpdateSessionManager>();
    }
}
