using System;
using System.Management.Automation;
using Etg.Yams.Azure.Storage;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Powershell
{
    [Cmdlet(VerbsCommon.Set, "DeploymentConfig")]
    [OutputType(typeof(DeploymentConfig))]
    public class SetDeploymentConfigCmdlet : Cmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "The connection string of the Yams storage")]
        public string ConnectionString { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "The deployment configuration to set")]
        public DeploymentConfig Config { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ConnectionString))
                {
                    throw new ArgumentException(nameof(ConnectionString));
                }

                int activityId = 0;
                var progressRecord = new ProgressRecord(activityId++, "Connect to blob storage",
                    "Connecting to blob storage");
                WriteProgress(progressRecord);
                var deploymentRepository = BlobStorageDeploymentRepository.Create(ConnectionString);
                progressRecord.RecordType = ProgressRecordType.Completed;
                WriteProgress(progressRecord);

                progressRecord = new ProgressRecord(activityId++, "PublishDeploymentConfig",
                    "Publishing DeploymentConfig.json to blob storage");
                deploymentRepository.PublishDeploymentConfig(Config).Wait();
                WriteProgress(progressRecord);

                WriteObject(Config);
            }
            catch (Exception e)
            {
                ThrowTerminatingError(new ErrorRecord(e, "0", ErrorCategory.OperationStopped, null));
            }
        }
    }
}
