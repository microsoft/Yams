using System;
using System.Collections.Generic;
using System.Management.Automation;
using Etg.Yams.Azure.Storage;
using Etg.Yams.Storage;
using Etg.Yams.Storage.Config;
using Etg.Yams.Storage.Status;

namespace Etg.Yams.Powershell
{
    [Cmdlet(VerbsCommon.Get, "DeploymentStatus")]
    [OutputType(typeof(DeploymentConfig))]
    public class GetDeploymentStatusCmdlet : Cmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "The connection string of the Yams storage")]
        public string ConnectionString { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "The id of the cluster")]
        public string ClusterId { get; set; }

        [Parameter(Mandatory = false, 
            HelpMessage = "Allow one to only show app that have been active within the last ActiveAgo seconds. " +
            "Default is 5 minutes")]
        public int ActiveAgo { get; set; } = 300;

        protected override void ProcessRecord()
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

            progressRecord = new ProgressRecord(activityId++, "FetchDeploymentConfig", "Fetching DeploymentConfig from storage");
            WriteProgress(progressRecord);
            var deploymentConfig = deploymentRepository.FetchDeploymentConfig().Result;
            progressRecord.RecordType = ProgressRecordType.Completed;
            WriteProgress(progressRecord);

            progressRecord = new ProgressRecord(activityId++, "FetchDeploymentStatus", $"Fetching DeploymentStatus");
            WriteProgress(progressRecord);
            var deploymentStatus = GetDeploymentStatus(deploymentRepository);
            progressRecord.RecordType = ProgressRecordType.Completed;
            WriteProgress(progressRecord);

            WriteObject(deploymentStatus);
        }

        private DeploymentStatus GetDeploymentStatus(BlobStorageDeploymentRepository deploymentRepository)
        {
            if (string.IsNullOrWhiteSpace(ClusterId))
            {
                return GetAllClustersDeploymentStatus(deploymentRepository);
            }
            return GetClusterDeploymentStatus(deploymentRepository, ClusterId);
        }

        private DeploymentStatus GetClusterDeploymentStatus(BlobStorageDeploymentRepository deploymentRepository, 
            string clusterId)
        {
            var apps = deploymentRepository.FetchClusterDeploymentStatus(clusterId, ttlSeconds: ActiveAgo).Result;
            return new DeploymentStatus(apps);
        }

        private DeploymentStatus GetAllClustersDeploymentStatus(BlobStorageDeploymentRepository deploymentRepository)
        {
            var deploymentConfig = deploymentRepository.FetchDeploymentConfig().Result;
            IEnumerable<string> clustersIds = deploymentConfig.ListClusters();
            var apps = new List<AppDeploymentStatus>();

            foreach (string clusterId in clustersIds)
            {
                var clusterStatus = deploymentRepository.FetchClusterDeploymentStatus(clusterId, ttlSeconds: ActiveAgo).Result;
                apps.AddRange(clusterStatus.ListAll());
            }
            return new DeploymentStatus(apps);
        }
    }
}