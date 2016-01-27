using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Storage
{
    public interface IDeploymentRepository
    {
        Task<DeploymentConfig> FetchDeploymentConfig();
        Task PublishDeploymentConfig(DeploymentConfig deploymentConfig);
        Task UploadApplicationBinaries(AppIdentity appIdentity, string localPath, ConflictResolutionMode conflictResolutionMode);
        Task DeleteApplicationBinaries(AppIdentity appIdentity);
        Task<bool> HasApplicationBinaries(AppIdentity appIdentity);
        Task DownloadApplicationBinaries(AppIdentity appIdentity, string localPath, ConflictResolutionMode conflictResolutionMode);
    }
}