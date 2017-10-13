using Etg.Yams.Application;
using Newtonsoft.Json;
using Semver;
using System;

namespace Etg.Yams.Storage.Status
{
    public class AppDeploymentStatus
    {
        public AppDeploymentStatus(AppIdentity appIdentity, string clusterId, string instanceId, 
            DateTime utcTimeStamp)
        {
            AppIdentity = appIdentity;
            ClusterId = clusterId;
            InstanceId = instanceId;
            UtcTimeStamp = utcTimeStamp;
        }

        [JsonConstructor]
        public AppDeploymentStatus(string id, string version, string clusterId, string instanceId, 
            DateTime utcTimeStamp) : this(new AppIdentity(id, version), clusterId, instanceId, utcTimeStamp)
        {
        }

        [JsonIgnore]
        public AppIdentity AppIdentity { get; private set; }

        public string Id => AppIdentity.Id;
        public string Version => AppIdentity.Version.ToString();
        public string ClusterId { get; }
        public string InstanceId { get; private set; }
        public DateTime UtcTimeStamp { get; }

        private AppDeploymentStatus Clone()
        {
            return new AppDeploymentStatus(AppIdentity, ClusterId, InstanceId, DateTime.UtcNow);
        }

        private AppDeploymentStatus WithVersion(SemVersion version)
        {
            var appDeploymentStatus = Clone();
            appDeploymentStatus.AppIdentity = new AppIdentity(appDeploymentStatus.AppIdentity.Id, version);
            return appDeploymentStatus;
        }
    }
}
