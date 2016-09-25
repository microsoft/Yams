using System.Collections.Generic;

namespace Etg.Yams
{
    /// <summary>
    /// Configuration parameters for YAMS.
    /// </summary>
    public class YamsConfig
    {
        public YamsConfig(string clusterId, string instanceUpdateDomain, string instanceId, 
            string applicationInstallDirectory, int checkForUpdatesPeriodInSeconds, int applicationRestartCount, 
            int processWaitForExitInSeconds, bool showApplicationProcessWindow, 
            IReadOnlyDictionary<string, string> clusterProperties)
        {
            ClusterId = clusterId;
            InstanceUpdateDomain = instanceUpdateDomain;
            InstanceId = instanceId;
            ApplicationInstallDirectory = applicationInstallDirectory;
            CheckForUpdatesPeriodInSeconds = checkForUpdatesPeriodInSeconds;
            ApplicationRestartCount = applicationRestartCount;
            ProcessWaitForExitInSeconds = processWaitForExitInSeconds;
            ShowApplicationProcessWindow = showApplicationProcessWindow;
            ClusterProperties = clusterProperties;
        }

        /// <summary>
        /// The cluster deployment id.
        /// </summary>
        public string ClusterId { get; private set; }

        /// <summary>
        /// The role instance update domain.
        /// </summary>
        public string InstanceUpdateDomain { get; private set; }

        /// <summary>
        /// The role instance id.
        /// </summary>
        public string InstanceId { get; private set; }

        /// <summary>
        /// The location on the role instance where applications will be installed.
        /// </summary>
        public string ApplicationInstallDirectory { get; private set; }

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

        public bool ShowApplicationProcessWindow { get; private set; }

        public IReadOnlyDictionary<string, string> ClusterProperties { get; private set; } 
    }
}