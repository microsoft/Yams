using Etg.Yams.Azure.Storage;
using Etg.Yams.Azure.UpdateSession;
using Etg.Yams.Update;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Etg.Yams.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            string clusterId = "testClusterId";
            string superClusterId = "testSuperClusterId";

            YamsConfig yamsConfig = new YamsConfigBuilder(
                    // mandatory configs
                    clusterId: clusterId,
                    instanceUpdateDomain: "0",
                    instanceId: "instance_0",
                    applicationInstallDirectory: Directory.GetCurrentDirectory())
                // optional configs
                .SetSuperClusterId(superClusterId)
                .SetCheckForUpdatesPeriodInSeconds(5)
                .SetApplicationRestartCount(int.MaxValue)
                .Build();

            string storageConnectionString = "UseDevelopmentStorage=true";

            //var yamsService = YamsServiceFactory.Create(yamsConfig,
            //    deploymentRepositoryStorageConnectionString: storageConnectionString,
            //    updateSessionStorageConnectionString: storageConnectionString);

            IUpdateSessionManager updateSessionManager = new AzureStorageUpdateSessionDiModule(
               yamsConfig.SuperClusterId,
               yamsConfig.ClusterId,
               yamsConfig.InstanceId,
               yamsConfig.InstanceUpdateDomain,
               storageConnectionString,
               yamsConfig.UpdateSessionTtl).UpdateSessionManager;

            BlobStorageDeploymentRepository deploymentRepository = BlobStorageDeploymentRepository.Create(storageConnectionString);

            var yamsService = YamsServiceFactory.Create(yamsConfig, deploymentRepository, deploymentRepository, updateSessionManager);

            try
            {
                Trace.TraceInformation("Yams is starting");
                await yamsService.Start();
                Trace.TraceInformation($"Yams has started. Looking for apps with clusterId: {clusterId}");
            }
            catch (Exception e)
            {
                Trace.TraceError($"Failed to start the Yams cluster {clusterId}", e);
                return;
            }

            while (true)
            {
                await Task.Delay(1000);
            }
        }
    }
}
