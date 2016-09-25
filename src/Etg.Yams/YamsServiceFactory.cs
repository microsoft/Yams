using Etg.Yams.Azure.Storage;
using Etg.Yams.Azure.UpdateSession;
using Etg.Yams.Json;
using Etg.Yams.Storage;
using Etg.Yams.Storage.Config;
using Etg.Yams.Update;
using Newtonsoft.Json.Serialization;

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

            IDeploymentConfigSerializer serializer = new JsonDeploymentConfigSerializer(new JsonSerializer(new DiagnosticsTraceWriter()));
            IDeploymentRepository deploymentRepository = new BlobStorageDeploymentRepository(deploymentRepositoryStorageConnectionString, serializer);
            return new YamsDiModule(yamsConfig, deploymentRepository, updateSessionManager).YamsService;
        }
    }
}
