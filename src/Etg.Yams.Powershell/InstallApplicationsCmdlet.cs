using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Azure.Storage;
using Etg.Yams.Storage;
using Etg.Yams.Storage.Config;
using Etg.Yams.Storage.Status;
using Newtonsoft.Json;

namespace Etg.Yams.Powershell
{

    [Cmdlet(VerbsLifecycle.Install, "Applications")]
    [OutputType(typeof(DeploymentConfig))]
    public class InstallApplicationsCmdlet : Cmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "The connection string of the Yams storage")]
        public string ConnectionString { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "The ids of the apps to publish")]
        public string[] AppsIds { get; set; }

        [Parameter(Mandatory = true, 
            HelpMessage = "The ids of the clusters; Entries order should correspond to entries in AppsIds")]
        public string[] ClustersIds { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "The new versions to publish" +
                                                   "Entries order should correspond to entries in AppsIds")]
        public string[] Versions { get; set; }

        [Parameter(HelpMessage = "The paths where binaries are located" +
                                                   "Entries order should correspond to entries in AppsIds")]
        public string[] BinariesPath { get; set; }

        [Parameter(HelpMessage = "Waits until the deployment status has been updated, " +
                                 "which indicates that the corresponding apps have been started")]
        public SwitchParameter WaitForDeploymentsToComplete { get; set; }

        [Parameter(HelpMessage = "Defines the behaviour when the binaries to upload already exist." +
                                 "The Default behaviour is to do nothing")]
        public ConflictResolutionMode BinariesConflictResolutionMode { get; set; } = ConflictResolutionMode.DoNothingIfBinariesExist;

        [Parameter(HelpMessage = "Indicates whether the DeploymentConfig.json should be uploaded" +
                                 "which will trigger deployment")]
        public SwitchParameter Deploy { get; set; } = true;

        [Parameter(HelpMessage = "Remove previously deployed version(s) of the app (Defaults to 'true')")]
        public SwitchParameter RemoveOldVersions { get; set; } = true;

        [Parameter(HelpMessage = "The properties of the clusters; Entries order should correspond to entries in AppsIds. " +
            "Example: {\"app1Prop1key\":\"app1Prop1Value\", \"app1Prop2Key\":\"app1Prop2Value\"}, {\"app2Prop1key\":\"app2Prop1Value\"}.")]
        public string[] Properties { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ConnectionString))
                {
                    throw new ArgumentException(nameof(ConnectionString));
                }
                if (ClustersIds?.Length != AppsIds.Length)
                {
                    throw new ArgumentException(nameof(ClustersIds));
                }
                if (Versions?.Length != AppsIds.Length)
                {
                    throw new ArgumentException(nameof(Versions));
                }
                if (BinariesPath?.Length != AppsIds.Length)
                {
                    throw new ArgumentException(nameof(BinariesPath));
                }
                if (AppsIds?.Length == 0)
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

                if (BinariesPath != null)
                {
                    var tasks = new List<Task>();
                    for (int i = 0; i < AppsIds.Length; ++i)
                    {
                        string appId = AppsIds[i];
                        string version = Versions[i];
                        string binariesPath = BinariesPath[i];

                        var newAppIdentity = new AppIdentity(appId, version);

                        if (BinariesPath != null)
                        {
                            tasks.Add(deploymentRepository.UploadApplicationBinaries(newAppIdentity, binariesPath,
                                BinariesConflictResolutionMode));
                        }
                    }
                    progressRecord = new ProgressRecord(activityId++, "UploadApplicationBinaries",
                        "Uploading binaries to blob storage");
                    WriteProgress(progressRecord);
                    Task.WhenAll(tasks).Wait();
                    progressRecord.RecordType = ProgressRecordType.Completed;
                    WriteProgress(progressRecord);
                }

                if (Deploy)
                {
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

                        if (RemoveOldVersions && deploymentConfig.HasApplication(appId))
                        {
                            deploymentConfig = deploymentConfig.RemoveApplication(appId, clusterId);
                        }

                        var newAppIdentity = new AppIdentity(appId, version);

                        if (!deploymentConfig.HasApplication(newAppIdentity, clusterId))
                        {
                            var appDeploymentConfig = deploymentConfig.HasApplication(newAppIdentity)? deploymentConfig.GetApplicationConfig(newAppIdentity) : new AppDeploymentConfig(newAppIdentity);
                            appDeploymentConfig = appDeploymentConfig.AddClusterId(clusterId);
                            if (Properties?.Length > i)
                            {
                                var appProperties = JsonConvert.DeserializeObject<Dictionary<string, string>>(Properties[i]);
                                foreach (var kvp in appProperties)
                                {
                                    appDeploymentConfig = appDeploymentConfig.AddProperty(kvp.Key, kvp.Value);
                                }
                            }

                            deploymentConfig = deploymentConfig.SetApplicationConfig(appDeploymentConfig);
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

                if (WaitForDeploymentsToComplete)
                {
                    var waitForDeploymentsProgressRecord = new ProgressRecord(activityId++,
                        "WaitForDeploymentsToComplete",
                        "Waiting for all deployments to complete");
                    WriteProgress(waitForDeploymentsProgressRecord);

                    List<AppInfo> pendingApps = new List<AppInfo>();
                    for (int i = 0; i < AppsIds.Length; ++i)
                    {
                        AppInfo app = new AppInfo
                        {
                            Id = AppsIds[i],
                            Version = Versions[i],
                            ClusterId = ClustersIds[i]
                        };
                        pendingApps.Add(app);
                    }

                    while (pendingApps.Any())
                    {
                        var appDeployments = new List<AppDeploymentStatus>();
                        var fetchTasks = new List<Task<ClusterDeploymentStatus>>();

                        progressRecord = new ProgressRecord(activityId++, "FetchClusterDeploymentStatus",
                            "Fetching deployments status");
                        WriteProgress(progressRecord);
                        foreach (string clusterId in ClustersIds)
                        {
                            fetchTasks.Add(
                                deploymentRepository.FetchClusterDeploymentStatus(clusterId, ttlSeconds: 60));
                        }
                        progressRecord.RecordType = ProgressRecordType.Completed;
                        WriteProgress(progressRecord);

                        ClusterDeploymentStatus[] result = Task.WhenAll(fetchTasks).Result;
                        foreach (var clusterDeploymentStatus in result)
                        {
                            appDeployments.AddRange(clusterDeploymentStatus.ListAll());
                        }

                        foreach (var appDeploymentStatus in appDeployments)
                        {
                            AppInfo app = pendingApps.FirstOrDefault(
                                appInfo => appInfo.ClusterId == appDeploymentStatus.ClusterId &&
                                           appInfo.Id == appDeploymentStatus.Id &&
                                           appInfo.Version == appDeploymentStatus.Version);
                            if (app != null)
                            {
                                pendingApps.Remove(app);
                                float quotient = (float) (AppsIds.Length - pendingApps.Count) / AppsIds.Length;
                                waitForDeploymentsProgressRecord.PercentComplete = (int) (100 * quotient);
                                WriteProgress(waitForDeploymentsProgressRecord);
                            }
                        }
                        Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                    }

                    waitForDeploymentsProgressRecord.RecordType = ProgressRecordType.Completed;
                    WriteProgress(waitForDeploymentsProgressRecord);
                }
            }
            catch (ArgumentException argException)
            {
                ThrowTerminatingError(new ErrorRecord(argException, "0", ErrorCategory.InvalidArgument, null));
            }
            catch (Exception e)
            {
                ThrowTerminatingError(new ErrorRecord(e, "0", ErrorCategory.OperationStopped, null));
            }
        }

        private class AppInfo
        {
            public string Id;
            public string Version;
            public string ClusterId;
        }
    }
}