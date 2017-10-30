using System;
using System.Linq;
using System.Management.Automation;
using Etg.Yams.Azure.Storage;

namespace Etg.Yams.Powershell
{
    [Cmdlet(VerbsCommon.Get, "ApplicationVersions")]
    [OutputType(typeof(string[]))]
    public class GetApplicationVersionsCmdlet : Cmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "The connection string of the Yams storage")]
        public string ConnectionString { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "The id of the app")]
        public string AppId { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "The id of the cluster")]
        public string ClusterId { get; set; }

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

                progressRecord = new ProgressRecord(activityId++, "FetchDeploymentConfig",
                    "Fetching DeploymentConfig.json from blob storage");
                WriteProgress(progressRecord);
                var deploymentConfig = deploymentRepository.FetchDeploymentConfig().Result;
                progressRecord.RecordType = ProgressRecordType.Completed;
                WriteProgress(progressRecord);

                var versions = from app in deploymentConfig
                               where app.AppIdentity.Id == AppId &&
                                     app.TargetClusters.Contains(ClusterId)
                               select app.AppIdentity.Version.ToString();

                WriteObject(versions.ToArray(), true);
            }
            catch (Exception e)
            {
                ThrowTerminatingError(new ErrorRecord(e, "0", ErrorCategory.OperationStopped, null));
            }
        }
    }
}
