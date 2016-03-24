using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Permissions;
using System.Threading.Tasks;
using Etg.Yams.WorkerRole.Utils;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Etg.Yams.WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private IYamsService _yamsService;

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public override void Run()
        {
            RunAsync().Wait();
        }

        public async Task RunAsync()
        {
            WorkerRoleConfig config = new WorkerRoleConfig();
            YamsConfig yamsConfig = new YamsConfigBuilder(
                // mandatory configs
                DeploymentIdUtils.CloudServiceDeploymentId,
                RoleEnvironment.CurrentRoleInstance.UpdateDomain.ToString(),
                RoleEnvironment.CurrentRoleInstance.Id,
                config.CurrentRoleInstanceLocalStoreDirectory)
                // optional configs
                .SetCheckForUpdatesPeriodInSeconds(config.UpdateFrequencyInSeconds)
                .SetApplicationRestartCount(config.ApplicationRestartCount)
                .Build();
            _yamsService = YamsServiceFactory.Create(yamsConfig,
                deploymentRepositoryStorageConnectionString: config.StorageDataConnectionString,
                updateSessionStorageConnectionString: config.StorageDataConnectionString);

            try
            {
                Trace.TraceInformation("Yams is starting");
                await _yamsService.Start();
                Trace.TraceInformation("Yams has started. Looking for apps with deploymentId:" + yamsConfig.ClusterDeploymentId);
                while (true)
                {
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        public override bool OnStart()
        {
            Trace.TraceInformation("Yams WorkerRole is starting");
            ServicePointManager.DefaultConnectionLimit = 1000;
            RoleEnvironment.Changing += RoleEnvironmentChanging;
            var result = base.OnStart();
            Trace.TraceInformation("Yams WorkerRole has started");
            return result;
        }

        public override void OnStop()
        {
            StopAsync().Wait();
        }

        public async Task StopAsync()
        {
            Trace.TraceInformation("Yams WorkerRole is stopping");
            RoleEnvironment.Changing -= RoleEnvironmentChanging;
            if (_yamsService != null)
            {
                await _yamsService.Stop();
            }
            base.OnStop();
            Trace.TraceInformation("Yams has stopped");
        }

        private void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            // If a configuration setting is changing);
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
            {
                e.Cancel = true;
            }
        }
    }
}
