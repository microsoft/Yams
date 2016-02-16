namespace Etg.Yams
{
    public class YamsConfigBuilder
    {
        public YamsConfigBuilder(string clusterDeploymentId, string instanceUpdateDomain, 
            string instanceId, string applicationInstallDirectory)
        {
            ClusterDeploymentId = clusterDeploymentId;
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

        public YamsConfig Build()
        {
            return new YamsConfig(ClusterDeploymentId, InstanceUpdateDomain, InstanceId, ApplicationInstallDirectory, 
                CheckForUpdatesPeriodInSeconds, ApplicationRestartCount, ProcessWaitForExitInSeconds, ShowApplicationProcessWindow);
        }

        public string ClusterDeploymentId { get; }
        public string InstanceUpdateDomain { get; }
        public string InstanceId { get; }
        public string ApplicationInstallDirectory { get; }
        public int CheckForUpdatesPeriodInSeconds { get; private set; }
        public int ApplicationRestartCount { get; private set; }
        public int ProcessWaitForExitInSeconds { get; private set; }
        public bool ShowApplicationProcessWindow { get; private set; }
    }
}