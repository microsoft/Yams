using Etg.Yams.Azure.Storage;
using Etg.Yams.Azure.UpdateSession;
using Etg.Yams.Storage;
using Etg.Yams.Update;

namespace Etg.Yams
{
    public class YamsServiceFactory
    {
        public static IYamsService Create(YamsConfig yamsConfig, string deploymentRepositoryStorageConnectionString,
            string updateSessionStorageConnectionString)
        {
            IUpdateSessionManager updateSessionManager = new AzureBlobStorageUpdateSessionDiModule(
                yamsConfig.ClusterDeploymentId,
                yamsConfig.InstanceId,
                yamsConfig.InstanceUpdateDomain,
                updateSessionStorageConnectionString).UpdateSessionManager;

            IDeploymentRepository deploymentRepository = new BlobStorageDeploymentRepository(deploymentRepositoryStorageConnectionString);
            return new YamsDiModule(yamsConfig, deploymentRepository, updateSessionManager).YamsService;
        }
    }
}
