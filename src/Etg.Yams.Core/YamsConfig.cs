using System;
using System.Collections.Generic;

namespace Etg.Yams
{
    /// <summary>
    /// Configuration parameters for YAMS.
    /// </summary>
    public class YamsConfig
    {
        public YamsConfig(
            string superClusterId,
            string clusterId,
            string instanceUpdateDomain,
            string instanceId,
            string applicationInstallDirectory,
            int checkForUpdatesPeriodInSeconds,
            int applicationRestartCount,
            int processWaitForExitInSeconds,
            bool useShellExecute,
            TimeSpan gracefulShutdownMessageTimeout,
            TimeSpan appGracefulShutdownTimeout,
            TimeSpan appHeartBeatTimeout,
            TimeSpan ipcConnectTimeout,
            TimeSpan appInitTimeout,
            TimeSpan updateSessionTtl,
            IReadOnlyDictionary<string, string> clusterProperties)
        {
            SuperClusterId = superClusterId;
            ClusterId = clusterId;
            InstanceUpdateDomain = instanceUpdateDomain;
            InstanceId = instanceId;
            ApplicationInstallDirectory = applicationInstallDirectory;
            CheckForUpdatesPeriodInSeconds = checkForUpdatesPeriodInSeconds;
            ApplicationRestartCount = applicationRestartCount;
            ProcessWaitForExitInSeconds = processWaitForExitInSeconds;
            UseShellExecute = useShellExecute;
            GracefulShutdownMessageTimeout = gracefulShutdownMessageTimeout;
            AppGracefulShutdownTimeout = appGracefulShutdownTimeout;
            AppHeartBeatTimeout = appHeartBeatTimeout;
            IpcConnectTimeout = ipcConnectTimeout;
            AppInitTimeout = appInitTimeout;
            UpdateSessionTtl = updateSessionTtl;
            ClusterProperties = clusterProperties;
        }

        /// <summary>
        /// An id that uniquely identifies a set of clusters (should be the same for all nodes in all clusters).
        /// You can use the clusterId if all your nodes are in the same cluster or if your clusters are independent.
        /// Note that the superClusterId is used by YAMS to enforce that only one update domain is active at a time;
        /// if your nodes have different superClusterId, they will update independently.
        /// </summary>
        public string SuperClusterId { get; }

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

        /// <summary>
        /// Whether the process is started with UseShellExecute or not
        /// </summary>
        [Obsolete("ShowApplicationProcessWindow is obsolete; use UseShellExecute")]
        public bool ShowApplicationProcessWindow { get => this.UseShellExecute; private set => this.UseShellExecute = value; }

        /// <summary>
        /// Whether the process is started with UseShellExecute or not
        /// </summary>
        public bool UseShellExecute { get; private set; }

        /// <summary>
        /// How long Yams should wait for the app to receive the graceful exit message
        /// </summary>
        public TimeSpan GracefulShutdownMessageTimeout { get; private set; }

        /// <summary>
        /// How long the app is given to exit after receiving the exit message
        /// </summary>
        public TimeSpan AppGracefulShutdownTimeout { get; private set; }

        /// <summary>
        /// Apps that are subscribed to health check periodically send heart beats messages to Yams.
        /// This property is the timeout after which Yams will declare the app unhealthy.
        /// </summary>
        public TimeSpan AppHeartBeatTimeout { get; private set; }

        /// <summary>
        /// Timeout for establishing an IPC connection with apps
        /// </summary>
        public TimeSpan IpcConnectTimeout { get; private set; }

        /// <summary>
        /// Time given to apps to finish initialization
        /// </summary>
        public TimeSpan AppInitTimeout { get; private set; }

        /// <summary>
        /// TTL after which an update session will be considered expired.
        /// </summary>
        public TimeSpan UpdateSessionTtl { get; }

        public IReadOnlyDictionary<string, string> ClusterProperties { get; private set; }
    }
}