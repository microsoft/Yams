using System.Collections.Generic;

namespace Etg.Yams.Storage.Config
{
    internal class VersionDeploymentConfig
    {
        public VersionDeploymentConfig(string version)
        {
            Version = version;
            DeploymentIds = new List<string>();
        }

        public VersionDeploymentConfig(string version, IEnumerable<string> deploymentIds)
        {
            Version = version;
            DeploymentIds = new SortedSet<string>(deploymentIds);
        }

        public VersionDeploymentConfig AddDeployment(string deploymentId)
        {
            var deploymentIds = new List<string>(DeploymentIds) {deploymentId};
            return new VersionDeploymentConfig(Version, deploymentIds);
        }

        public VersionDeploymentConfig RemoveDeployment(string deploymentId)
        {
            HashSet<string> deploymentIds = new HashSet<string>(DeploymentIds);
            deploymentIds.Remove(deploymentId);
            return new VersionDeploymentConfig(Version, deploymentIds);
        }

        public string Version { get; }

        public IEnumerable<string> DeploymentIds { get; }
    }
}