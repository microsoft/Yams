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

            var yamsService = YamsServiceBuilder
                .WithConfig(yamsConfig)
                .UsingAzureTableUpdateSessionManager(storageConnectionString)
                .UsingBlobStorageDeploymentRepository(storageConnectionString)
                .Build();

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
