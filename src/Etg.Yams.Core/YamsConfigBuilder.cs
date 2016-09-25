using System.Collections.Generic;

namespace Etg.Yams
{
    public class YamsConfigBuilder
    {
        public YamsConfigBuilder(string clusterId, string instanceUpdateDomain, 
            string instanceId, string applicationInstallDirectory)
        {
            ClusterId = clusterId;
            InstanceUpdateDomain = instanceUpdateDomain;
            InstanceId = instanceId;
            ApplicationInstallDirectory = applicationInstallDirectory;
            CheckForUpdatesPeriodInSeconds = 10;
            ApplicationRestartCount = 3;
            ProcessWaitForExitInSeconds = 5;
            ShowApplicationProcessWindow = true;
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

        public YamsConfigBuilder SetShowApplicationProcessWindow(bool value)
        {
            ShowApplicationProcessWindow = value;
            return this;
        }

        public YamsConfigBuilder AddClusterProperty(string key, string value)
        {
            ClusterProperties[key] = value;
            return this;
        }

        public YamsConfig Build()
        {
            return new YamsConfig(ClusterId, InstanceUpdateDomain, InstanceId, ApplicationInstallDirectory, 
                CheckForUpdatesPeriodInSeconds, ApplicationRestartCount, ProcessWaitForExitInSeconds, 
                ShowApplicationProcessWindow, ClusterProperties);
        }

        //TODO: Make the following properties private
        public string ClusterId { get; }
        public string InstanceUpdateDomain { get; }
        public string InstanceId { get; }
        public string ApplicationInstallDirectory { get; }
        public int CheckForUpdatesPeriodInSeconds { get; private set; }
        public int ApplicationRestartCount { get; private set; }
        public int ProcessWaitForExitInSeconds { get; private set; }
        public bool ShowApplicationProcessWindow { get; private set; }
        public Dictionary<string, string> ClusterProperties { get; private set; } = new Dictionary<string, string>();
    }
}