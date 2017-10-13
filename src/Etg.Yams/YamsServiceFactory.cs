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
                yamsConfig.ClusterId,
                yamsConfig.InstanceId,
                yamsConfig.InstanceUpdateDomain,
                updateSessionStorageConnectionString).UpdateSessionManager;

            var deploymentRepository = BlobStorageDeploymentRepository.Create(deploymentRepositoryStorageConnectionString);
            return new YamsDiModule(yamsConfig, deploymentRepository, deploymentRepository, updateSessionManager).YamsService;
        }
    }
}
