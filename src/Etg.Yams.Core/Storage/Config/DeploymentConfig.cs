using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Etg.Yams.Application;

namespace Etg.Yams.Storage.Config
{
    public class DeploymentConfig : IEnumerable<AppDeploymentConfig>
    {
        private readonly ISet<AppDeploymentConfig> _apps = new HashSet<AppDeploymentConfig>();

        public DeploymentConfig()
        {
        }

        protected bool Equals(DeploymentConfig other)
        {
            return Equals(_apps, other._apps);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DeploymentConfig) obj);
        }

        public override int GetHashCode()
        {
            return (_apps != null ? _apps.GetHashCode() : 0);
        }

        public DeploymentConfig(IEnumerable<AppDeploymentConfig> apps)
        {
            _apps = new HashSet<AppDeploymentConfig>(apps);
        }

        public IEnumerable<string> ListApplications()
        {
            var appIds = new HashSet<string>();
            appIds.UnionWith(_apps.Select(config => config.AppIdentity.Id));
            return appIds;
        }

        public IEnumerable<string> ListApplications(string clusterId)
        {
            var appIds = new HashSet<string>();
            appIds.UnionWith(_apps.Where(config => config.TargetClusters.Contains(clusterId))
                .Select(config => config.AppIdentity.Id));
            return appIds;
        }

        public IEnumerable<string> ListVersions(string appId)
        {
            var appIds = new HashSet<string>();
            appIds.UnionWith(_apps
                .Where(config => config.AppIdentity.Id == appId)
                .Select(config => config.AppIdentity.Version.ToString()));
            return appIds;
        }

        public IEnumerable<string> ListVersions(string appId, string clusterId)
        {
            var appIds = new HashSet<string>();
            appIds.UnionWith(_apps
                .Where(config => config.AppIdentity.Id == appId && config.TargetClusters.Contains(clusterId))
                .Select(config => config.AppIdentity.Version.ToString()));
            return appIds;
        }

        public IEnumerable<string> ListClusters(string appId)
        {
            var clusterIds = new HashSet<string>();
            clusterIds.UnionWith(_apps.Where(config => config.AppIdentity.Id == appId).SelectMany(config => config.TargetClusters));
            return clusterIds;
        }

        public IEnumerable<string> ListClusters(AppIdentity appIdentity)
        {
            var clusterIds = new HashSet<string>();
            clusterIds.UnionWith(_apps.Where(config => config.AppIdentity == appIdentity).SelectMany(config => config.TargetClusters));
            return clusterIds;
        }

        public DeploymentConfig AddApplication(AppIdentity appIdentity, string clusterId)
        {
            AppDeploymentConfig appDeploymentConfig;
            DeploymentConfig deploymentConfig = this;
            if (deploymentConfig.HasApplication(appIdentity))
            {
                appDeploymentConfig = deploymentConfig.GetAppConfig(appIdentity);
                appDeploymentConfig = appDeploymentConfig.AddClusterId(clusterId);
                deploymentConfig = RemoveApplication(appIdentity);
            }
            else
            {
                appDeploymentConfig = new AppDeploymentConfig(appIdentity, new [] {clusterId});
            }
            return deploymentConfig.AddApplication(appDeploymentConfig);
        }

        public DeploymentConfig AddApplication(AppDeploymentConfig appDeploymentConfig)
        {
            var apps = new HashSet<AppDeploymentConfig>(_apps) {appDeploymentConfig};
            return new DeploymentConfig(apps);
        }

        public AppDeploymentConfig GetAppConfig(AppIdentity appIdentity)
        {
            IEnumerable<AppDeploymentConfig> appConfigs = _apps.Where(config => config.AppIdentity == appIdentity);
            if (!appConfigs.Any())
            {
                throw new InvalidOperationException($"App {appIdentity} doesn't exist");
            }
            if (appConfigs.Count() > 1)
            {
                throw new InvalidOperationException($"There should not be more than one app for a given {nameof(AppIdentity)}");
            }
            return appConfigs.First();
        }

        public bool HasApplication(string appId)
        {
            return _apps.Any(appDeploymentConfig => appDeploymentConfig.AppIdentity.Id == appId);
        }

        public bool HasApplication(AppIdentity appIdentity)
        {
            return _apps.Any(appDeploymentConfig => appDeploymentConfig.AppIdentity == appIdentity);
        }

        public bool HasApplication(AppIdentity appIdentity, string clusterId)
        {
            return _apps.Any(
                appDeploymentConfig => appDeploymentConfig.AppIdentity == appIdentity && 
                appDeploymentConfig.TargetClusters.Contains(clusterId));
        }

        public DeploymentConfig RemoveApplication(string appId)
        {
            if (!HasApplication(appId))
            {
                throw new InvalidOperationException("Cannot remove an application that is not there");
            }
            var apps = new HashSet<AppDeploymentConfig>(_apps);
            apps.RemoveWhere(config => config.AppIdentity.Id == appId);
            return new DeploymentConfig(apps);
        }

        public DeploymentConfig RemoveApplication(AppIdentity appIdentity)
        {
            if (!HasApplication(appIdentity))
            {
                throw new InvalidOperationException("Cannot remove an application that is not there");
            }
            var apps = new HashSet<AppDeploymentConfig>(_apps);
            apps.RemoveWhere(config => config.AppIdentity == appIdentity);
            return new DeploymentConfig(apps);
        }

        public DeploymentConfig RemoveApplication(AppIdentity appIdentity, string clusterId)
        {
            if (!HasApplication(appIdentity, clusterId))
            {
                throw new InvalidOperationException("Cannot remove an application that is not there");
            }
            var apps = new HashSet<AppDeploymentConfig>(_apps);
            apps.RemoveWhere(config => config.AppIdentity == appIdentity && config.TargetClusters.Contains(clusterId));
            return new DeploymentConfig(apps);
        }

        public IEnumerator<AppDeploymentConfig> GetEnumerator()
        {
            return _apps.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}