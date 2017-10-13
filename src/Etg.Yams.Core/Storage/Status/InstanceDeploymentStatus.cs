using Etg.Yams.Application;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Etg.Yams.Storage.Status
{
    public class InstanceDeploymentStatus
    {
        private readonly Dictionary<AppIdentity, AppDeploymentStatus> _apps = new Dictionary<AppIdentity, AppDeploymentStatus>();

        public InstanceDeploymentStatus()
        {
        }

        [JsonConstructor]
        public InstanceDeploymentStatus(IEnumerable<AppDeploymentStatus> applications)
        {
            if (applications == null)
            {
                return;
            }
            foreach (AppDeploymentStatus appDeploymentStatus in applications)
            {
                SetAppDeploymentStatus(appDeploymentStatus);
            }
        }

        public IEnumerable<AppDeploymentStatus> Applications =>_apps.Values;

        public AppDeploymentStatus GetAppDeploymentStatus(AppIdentity appIdentity)
        {
            AppDeploymentStatus appDeploymentStatus;
            if (!_apps.TryGetValue(appIdentity, out appDeploymentStatus))
            {
                return null;
            }
            return appDeploymentStatus;
        }

        public void SetAppDeploymentStatus(AppDeploymentStatus appDeploymentStatus)
        {
            _apps[appDeploymentStatus.AppIdentity] = appDeploymentStatus;
        }
    }
}
