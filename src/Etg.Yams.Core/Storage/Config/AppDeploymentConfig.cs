using System;
using System.Collections.Generic;
using System.Linq;
using Etg.Yams.Application;

namespace Etg.Yams.Storage.Config
{
    public class AppDeploymentConfig : AppInstallConfig
    {
        public AppDeploymentConfig(AppIdentity appIdentity, IEnumerable<string> clustersIds,
            IReadOnlyDictionary<string, string> properties) : base(appIdentity, properties)
        {
            ClustersIds = new HashSet<string>(clustersIds);
        }

        public AppDeploymentConfig(AppIdentity appIdentity, IEnumerable<string> clustersIds) : this(appIdentity, clustersIds,
            new Dictionary<string, string>())
        {
        }

        public AppDeploymentConfig AddClusterId(string clusterId)
        {
            if (ClustersIds.Contains(clusterId))
            {
                throw new InvalidOperationException();
            }
            var clusterIds = new HashSet<string>(ClustersIds) {clusterId};
            return new AppDeploymentConfig(AppIdentity, clusterIds, Properties);
        }

        protected bool Equals(AppDeploymentConfig other)
        {
            return base.Equals(other) && (new HashSet<string>(ClustersIds).SetEquals(other.ClustersIds));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AppDeploymentConfig) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode()*397) ^ (ClustersIds != null ? ClustersIds.GetHashCode() : 0);
            }
        }

        public static bool operator ==(AppDeploymentConfig left, AppDeploymentConfig right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AppDeploymentConfig left, AppDeploymentConfig right)
        {
            return !Equals(left, right);
        }

        public IEnumerable<string> ClustersIds { get; }
    }
}