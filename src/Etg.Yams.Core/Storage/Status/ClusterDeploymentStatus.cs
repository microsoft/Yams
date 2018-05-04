using Etg.Yams.Application;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Etg.Yams.Storage.Status
{
    public class ClusterDeploymentStatus : IEnumerable<AppDeploymentStatus>
    {
        readonly Dictionary<string, InstanceDeploymentStatus> _instances = new Dictionary<string, InstanceDeploymentStatus>();

        public AppDeploymentStatus GetAppDeploymentStatus(string instanceId, AppIdentity appIdentity)
        {
            InstanceDeploymentStatus instanceDeploymentStatus;
            if (!_instances.TryGetValue(instanceId, out instanceDeploymentStatus))
            {
                return null;
            }
            return instanceDeploymentStatus.GetAppDeploymentStatus(appIdentity);
        }

        public void SetAppDeploymentStatus(AppDeploymentStatus appDeploymentStatus)
        {
            InstanceDeploymentStatus instanceDeploymentStatus;
            if (!_instances.TryGetValue(appDeploymentStatus.InstanceId, out instanceDeploymentStatus))
            {
                instanceDeploymentStatus = new InstanceDeploymentStatus();
                _instances[appDeploymentStatus.InstanceId] = instanceDeploymentStatus;
            }
            instanceDeploymentStatus.SetAppDeploymentStatus(appDeploymentStatus);
        }

        public void SetInstanceDeploymentStatus(string instanceId, InstanceDeploymentStatus status)
        {
            _instances[instanceId] = status;
        }

        public IEnumerable<AppDeploymentStatus> ListAll()
        {
            return _instances.Values.SelectMany(instance => instance.Applications);
        }

        public IEnumerator<AppDeploymentStatus> GetEnumerator()
        {
            return ListAll().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
