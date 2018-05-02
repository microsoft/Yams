using System;
using Etg.Yams.Azure.Storage;
using Etg.Yams.Configuration;
using Etg.Yams.NuGet.Storage;
using Etg.Yams.Storage;

namespace Etg.Yams
{
    public static class NugetFeedDeploymentRepositoryConfigurator
    {
        public static IWithDeploymentRepository UsingNugetFeedDeploymentRepository(this IWithUpdateSessionManager builder, string connectionString, string feedUrl, NugetFeedCredentials credentials = null, INugetPackageExtractor extractor = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            if (feedUrl == null)
                throw new ArgumentNullException(nameof(feedUrl));

            if (builder is WithUpdateSessionManager x)
            {
                var deploymentRepository = BlobStorageDeploymentRepository.Create(connectionString);
                var nugetApplicationRepository = new NugetFeedApplicationRepository(extractor ?? new NugetPackageExtractor(), feedUrl, credentials);

                return new WithDeploymentRepository(x.Config, x.UpdateSessionManager, new DeploymentRepository(deploymentRepository, nugetApplicationRepository), deploymentRepository);
            }

            throw new NotSupportedException($"Expecting {typeof(IWithDeploymentRepository).FullName} to be implemented by {typeof(WithUpdateSessionManager).FullName}. Instead received an instance of {builder.GetType().FullName}.");
        }
    }
}