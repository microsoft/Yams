namespace Etg.Yams
{
    public class YamsConfigBuilder
    {
        public YamsConfigBuilder(string storageDataConnectionString, string cloudServiceDeploymentId, string instanceUpdateDomain, 
            string currentRoleInstanceId, string applicationInstallDirectory)
        {
            StorageDataConnectionString = storageDataConnectionString;
            CloudServiceDeploymentId = cloudServiceDeploymentId;
            InstanceUpdateDomain = instanceUpdateDomain;
            CurrentRoleInstanceId = currentRoleInstanceId;
            ApplicationInstallDirectory = applicationInstallDirectory;
            StorageBlobLeaseRenewIntervalInSeconds = 60;
            CheckForUpdatesPeriodInSeconds = 10;
            ApplicationRestartCount = 3;
            ProcessWaitForExitInSeconds = 5;
        }

        public YamsConfigBuilder SetStorageBlobLeaseRenewIntervalInSeconds(int value)
        {
            StorageBlobLeaseRenewIntervalInSeconds = value;
            return this;
        }

        public YamsConfigBuilder SetCheckForUpdatesPeriodInSeconds(int value)
        {
            CheckForUpdatesPeriodInSeconds = value;
            return this;
        }

        public YamsConfigBuilder SetApplicationRestartCount(int value)
        {
            ApplicationRestartCount = value;
            return this;
        }

        public YamsConfigBuilder SetProcessWaitForExitInSeconds(int value)
        {
            ProcessWaitForExitInSeconds = value;
            return this;
        }

        public YamsConfig Build()
        {
            return new YamsConfig(StorageDataConnectionString, CloudServiceDeploymentId, InstanceUpdateDomain, CurrentRoleInstanceId, ApplicationInstallDirectory, StorageBlobLeaseRenewIntervalInSeconds, CheckForUpdatesPeriodInSeconds, ApplicationRestartCount, ProcessWaitForExitInSeconds);
        }

        public string StorageDataConnectionString { get; private set; }
        public string CloudServiceDeploymentId { get; private set; }
        public string InstanceUpdateDomain { get; private set; }
        public string CurrentRoleInstanceId { get; private set; }
        public string ApplicationInstallDirectory { get; private set; }
        public int StorageBlobLeaseRenewIntervalInSeconds { get; private set; }
        public int CheckForUpdatesPeriodInSeconds { get; private set; }
        public int ApplicationRestartCount { get; private set; }
        public int ProcessWaitForExitInSeconds { get; private set; }
    }
}