using System.Linq;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Deploy
{
    public class ClusterIdDeploymentMatcher : IAppDeploymentMatcher
    {
        private readonly string _clusterId;

        public ClusterIdDeploymentMatcher(string clusterId)
        {
            _clusterId = clusterId;
        }

        public bool IsMatch(AppDeploymentConfig appDeploymentConfig)
        {
            return appDeploymentConfig.TargetClusters.Contains(_clusterId);
        }
    }
}