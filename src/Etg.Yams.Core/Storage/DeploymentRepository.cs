using System;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Storage
{
    public class DeploymentRepository : IDeploymentRepository
    {
        private readonly IDeploymentConfigRepository _deploymentConfigRepository;
        private readonly IApplicationRepository _applicationRepository;

        public DeploymentRepository(IDeploymentConfigRepository deploymentConfigRepository, IApplicationRepository applicationRepository)
        {
            _deploymentConfigRepository = deploymentConfigRepository;
            _applicationRepository = applicationRepository;
        }

        public Task DeleteApplicationBinaries(AppIdentity appIdentity)
        {
            return _applicationRepository.DeleteApplicationBinaries(appIdentity);
        }

        public Task DownloadApplicationBinaries(AppIdentity appIdentity, string localPath, ConflictResolutionMode conflictResolutionMode)
        {
            return _applicationRepository.DownloadApplicationBinaries(appIdentity, localPath, conflictResolutionMode);
        }

        public Task<DeploymentConfig> FetchDeploymentConfig()
        {
            return _deploymentConfigRepository.FetchDeploymentConfig();
        }

        public Task<bool> HasApplicationBinaries(AppIdentity appIdentity)
        {
            return _applicationRepository.HasApplicationBinaries(appIdentity);
        }

        public Task PublishDeploymentConfig(DeploymentConfig deploymentConfig)
        {
            return _deploymentConfigRepository.PublishDeploymentConfig(deploymentConfig);
        }

        public Task UploadApplicationBinaries(AppIdentity appIdentity, string localPath, ConflictResolutionMode conflictResolutionMode)
        {
            return _applicationRepository.UploadApplicationBinaries(appIdentity, localPath, conflictResolutionMode);
        }
    }
}
