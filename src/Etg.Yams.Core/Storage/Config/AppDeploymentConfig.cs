using System;
using System.Collections.Generic;
using System.Linq;
using Etg.Yams.Application;
using Etg.Yams.Install;
using Etg.Yams.Utils;

namespace Etg.Yams.Storage.Config
{
    public class AppDeploymentConfig : AppInstallConfig
    {
        public AppDeploymentConfig(AppIdentity appIdentity) : base(appIdentity)
        {
        }

        public AppDeploymentConfig(AppIdentity appIdentity, IEnumerable<string> targetClusters,
            IReadOnlyDictionary<string, string> properties) : base(appIdentity, properties)
        {
            TargetClusters = new HashSet<string>(targetClusters);
        }

        public AppDeploymentConfig(AppIdentity appIdentity, IEnumerable<string> clustersIds) : this(appIdentity, clustersIds,
            new Dictionary<string, string>())
        {
        }

        public AppDeploymentConfig AddClusterId(string clusterId)
        {
            if (TargetClusters.Contains(clusterId))
            {
                throw new InvalidOperationException();
            }
            var clusterIds = new HashSet<string>(TargetClusters) {clusterId};
            return new AppDeploymentConfig(AppIdentity, clusterIds, Properties);
        }

        public AppDeploymentConfig RemoveClusterId(string clusterId)
        {
            if (!TargetClusters.Contains(clusterId))
            {
                throw new InvalidOperationException();
            }
            var clusterIds = TargetClusters.Where(c => c != clusterId);
            return new AppDeploymentConfig(AppIdentity, clusterIds, Properties);
        }

        public AppDeploymentConfig AddProperty(string key, string value)
        {
            if (Properties.ContainsKey(key))
            {
                return this;
            }
            Dictionary<string, string> dict = Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            dict[key] = value;
            return new AppDeploymentConfig(AppIdentity, TargetClusters, dict);
        }

        public AppDeploymentConfig RemoveProperty(string key)
        {
            if (Properties.ContainsKey(key))
            {
                return this;
            }
            Dictionary<string, string> dict = Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            dict.Remove(key);
            return new AppDeploymentConfig(AppIdentity, TargetClusters, dict);
        }

        protected new bool Equals(AppDeploymentConfig other)
        {
            return base.Equals(other) && (new HashSet<string>(TargetClusters).SetEquals(other.TargetClusters));
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
                int hashCode = (base.GetHashCode()*397) ^ (TargetClusters != null ? HashCodeUtils.GetHashCode(TargetClusters) : 0);
                return hashCode;
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

        public IEnumerable<string> TargetClusters { get; }
    }
}