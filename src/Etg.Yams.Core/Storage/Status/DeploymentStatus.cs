using Etg.Yams.Application;
using System.Collections.Generic;
using System.Linq;

namespace Etg.Yams.Storage.Status
{
    public class DeploymentStatus
    {
        private readonly Dictionary<string, ClusterDeploymentStatus> _clusters = new Dictionary<string, ClusterDeploymentStatus>();

        public DeploymentStatus()
        {
        }
        public DeploymentStatus(IEnumerable<AppDeploymentStatus> apps)
        {
            foreach(AppDeploymentStatus appDeploymentStatus in apps)
            {
                SetAppDeploymentStatus(appDeploymentStatus);
            }
        }

        public void SetClusterDeploymentStatus(string clusterId, ClusterDeploymentStatus clusterDeploymentStatus)
        {
            _clusters[clusterId] = clusterDeploymentStatus;
        }

        public AppDeploymentStatus GetAppDeploymentStatus(string clusterId, string instanceId, AppIdentity appIdentity)
        {
            ClusterDeploymentStatus clusterDeploymentStatus;
            if (!_clusters.TryGetValue(clusterId, out clusterDeploymentStatus))
            {
                return null;
            }
            return clusterDeploymentStatus.GetAppDeploymentStatus(instanceId, appIdentity);
        }

        public void SetAppDeploymentStatus(AppDeploymentStatus appDeploymentStatus)
        {
            ClusterDeploymentStatus clusterDeploymentStatus;
            if (!_clusters.TryGetValue(appDeploymentStatus.ClusterId, out clusterDeploymentStatus))
            {
                clusterDeploymentStatus = new ClusterDeploymentStatus();
                _clusters[appDeploymentStatus.ClusterId] = clusterDeploymentStatus;
            }
            clusterDeploymentStatus.SetAppDeploymentStatus(appDeploymentStatus);
        }

        public ClusterDeploymentStatus GetClusterDeploymentStatus(string clusterId)
        {
            ClusterDeploymentStatus clusterDeploymentStatus;
            if (!_clusters.TryGetValue(clusterId, out clusterDeploymentStatus))
            {
                return null;
            }
            return clusterDeploymentStatus;
        }

        public IEnumerable<AppDeploymentStatus> ListAll()
        {
            return _clusters.Values.SelectMany(cluster => cluster.ListAll());
        }
    }
}
