using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Storage
{
    public interface IYamsRepository
    {
        Task<DeploymentConfig> FetchDeploymentConfig();
        Task PublishDeploymentConfig(DeploymentConfig deploymentConfig);
        Task UploadApplicationBinaries(AppIdentity appIdentity, string localPath, FileMode fileMode);
        Task DeleteApplicationBinaries(AppIdentity appIdentity);
        Task<bool> HasApplicationBinaries(AppIdentity appIdentity);
        Task DownloadApplicationBinaries(AppIdentity appIdentity, string localPath, FileMode fileMode);
    }
}