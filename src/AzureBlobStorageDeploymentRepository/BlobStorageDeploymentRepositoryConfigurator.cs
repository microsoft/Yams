using System;
using Etg.Yams.Azure.Storage;
using Etg.Yams.Configuration;

namespace Etg.Yams
{
    public static class BlobStorageDeploymentRepositoryConfigurator
    {
        public static IWithDeploymentRepository UsingBlobStorageDeploymentRepository(this IWithUpdateSessionManager builder, string connectionString)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (builder is WithUpdateSessionManager x)
            {
                var deploymentRepository = BlobStorageDeploymentRepository.Create(connectionString);

                return new WithDeploymentRepository(x.Config, x.UpdateSessionManager, deploymentRepository, deploymentRepository);
            }

            throw new NotSupportedException($"Expecting {typeof(IWithDeploymentRepository).FullName} to be implemented by {typeof(WithUpdateSessionManager).FullName}. Instead received an instance of {builder.GetType().FullName}.");
        }
    }
}