using System;
using System.Management.Automation;
using Etg.Yams.Application;
using Etg.Yams.Azure.Storage;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Powershell
{
    [Cmdlet(VerbsLifecycle.Uninstall, "Applications")]
    [OutputType(typeof(DeploymentConfig))]
    public class UninstallApplicationsCmdlet : Cmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "The connection string of the Yams storage")]
        public string ConnectionString { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "The ids of the apps to uninstall")]
        public string[] AppsIds { get; set; }

        [Parameter(Mandatory = true,
            HelpMessage = "The ids of the clusters; Entries order should correspond to entries in AppsIds")]
        public string[] ClustersIds { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "The versions to uninstall. " +
                                                   "Entries order should correspond to entries in AppsIds")]
        public string[] Versions { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ConnectionString))
                {
                    throw new ArgumentException(nameof(ConnectionString));
                }
                if (ClustersIds.Length != AppsIds.Length)
                {
                    throw new ArgumentException(nameof(ClustersIds));
                }
                if (Versions.Length != AppsIds.Length)
                {
                    throw new ArgumentException(nameof(Versions));
                }
                if (AppsIds.Length == 0)
                {
                    throw new ArgumentException(nameof(AppsIds));
                }

                int activityId = 0;
                var progressRecord = new ProgressRecord(activityId++, "Connect to blob storage",
                    "Connecting to blob storage");
                WriteProgress(progressRecord);
                var deploymentRepository = BlobStorageDeploymentRepository.Create(ConnectionString);
                progressRecord.RecordType = ProgressRecordType.Completed;
                WriteProgress(progressRecord);

                progressRecord = new ProgressRecord(activityId++, "FetchDeploymentConfig",
                    "Fetching DeploymentConfig.json from blob storage");
                WriteProgress(progressRecord);
                var deploymentConfig = deploymentRepository.FetchDeploymentConfig().Result;
                progressRecord.RecordType = ProgressRecordType.Completed;
                WriteProgress(progressRecord);

                for (int i = 0; i < AppsIds.Length; ++i)
                {
                    string appId = AppsIds[i];
                    string version = Versions[i];
                    string clusterId = ClustersIds[i];

                    var toRemove = new AppIdentity(appId, version);
                    if (deploymentConfig.HasApplication(toRemove))
                    {
                        deploymentConfig = deploymentConfig.RemoveApplication(toRemove);
                    }
                }

                progressRecord = new ProgressRecord(activityId++, "PublishDeploymentConfig",
                    "Publishing DeploymentConfig.json to blob storage");
                WriteProgress(progressRecord);
                deploymentRepository.PublishDeploymentConfig(deploymentConfig).Wait();
                progressRecord.RecordType = ProgressRecordType.Completed;
                WriteProgress(progressRecord);

                WriteObject(deploymentConfig);
            }
            catch (Exception e)
            {
                ThrowTerminatingError(new ErrorRecord(e, "0", ErrorCategory.OperationStopped, null));
            }
        }
    }
}
