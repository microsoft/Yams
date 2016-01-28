namespace Etg.Yams
{
    /// <summary>
    /// Configuration parameters for YAMS.
    /// </summary>
    public class YamsConfig
    {
        public YamsConfig(string storageDataConnectionString, string cloudServiceDeploymentId, string instanceUpdateDomain, string roleInstanceId, 
            string applicationInstallDirectory, int storageBlobLeaseRenewIntervalInSeconds, int checkForUpdatesPeriodInSeconds, int applicationRestartCount, 
            int processWaitForExitInSeconds)
        {
            StorageDataConnectionString = storageDataConnectionString;
            CloudServiceDeploymentId = cloudServiceDeploymentId;
            InstanceUpdateDomain = instanceUpdateDomain;
            RoleInstanceId = roleInstanceId;
            ApplicationInstallDirectory = applicationInstallDirectory;
            StorageBlobLeaseRenewIntervalInSeconds = storageBlobLeaseRenewIntervalInSeconds;
            CheckForUpdatesPeriodInSeconds = checkForUpdatesPeriodInSeconds;
            ApplicationRestartCount = applicationRestartCount;
            ProcessWaitForExitInSeconds = processWaitForExitInSeconds;
        }

        /// <summary>
        /// Data connection string used to connect to the blob storage.
        /// </summary>
        public string StorageDataConnectionString { get; private set; }

        /// <summary>
        /// The cluster deployment id.
        /// </summary>
        public string CloudServiceDeploymentId { get; private set; }

        /// <summary>
        /// The role instance update domain.
        /// </summary>
        public string InstanceUpdateDomain { get; private set; }

        /// <summary>
        /// The role instance id.
        /// </summary>
        public string RoleInstanceId { get; private set; }

        /// <summary>
        /// The location on the role instance where applications will be installed.
        /// </summary>
        public string ApplicationInstallDirectory { get; private set; }

        /// <summary>
        /// Used to renew the blob lease. This parameter is optional; the default value works fine.
        /// </summary>
        public int StorageBlobLeaseRenewIntervalInSeconds { get; private set; }

        /// <summary>
        /// How frequently to check for updates (in seconds).
        /// </summary>
        public int CheckForUpdatesPeriodInSeconds { get; private set; }

        /// <summary>
        /// How many times should YAMS attempt to restart and application that fails.
        /// </summary>
        public int ApplicationRestartCount { get; private set; }

        /// <summary>
        /// How long should we wait after an application is stopped (to give it time to exit and free resources).
        /// </summary>
        public int ProcessWaitForExitInSeconds { get; private set; }
    }
}