using System.Threading.Tasks;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Storage
{
    public interface IDeploymentConfigRepository
    {
        Task<DeploymentConfig> FetchDeploymentConfig();
        Task PublishDeploymentConfig(DeploymentConfig deploymentConfig);
    }
}