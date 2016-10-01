using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Etg.Yams.Application;
using Etg.Yams.Utils;

namespace Etg.Yams.Storage.Config
{
    public class DeploymentConfig : IEnumerable<AppDeploymentConfig>
    {
        private readonly Dictionary<AppIdentity, AppDeploymentConfig> _apps = new Dictionary<AppIdentity, AppDeploymentConfig>();

        public DeploymentConfig()
        {
        }

        public DeploymentConfig(IEnumerable<AppDeploymentConfig> apps)
        {
            _apps = new HashSet<AppDeploymentConfig>(apps).ToDictionary(config => config.AppIdentity, config => config);
        }

        private DeploymentConfig(Dictionary<AppIdentity, AppDeploymentConfig> apps)
        {
            _apps = apps;
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
            return HashCodeUtils.GetHashCode(_apps.Values);
        }

        public IEnumerable<string> ListApplications()
        {
            var appIds = new HashSet<string>();
            appIds.UnionWith(_apps.Keys.Select(identity => identity.Id));
            return appIds;
        }

        public IEnumerable<string> ListApplications(string clusterId)
        {
            var appIds = new HashSet<string>();
            appIds.UnionWith(_apps.Values.Where(config => config.TargetClusters.Contains(clusterId))
                .Select(config => config.AppIdentity.Id));
            return appIds;
        }

        public IEnumerable<string> ListVersions(string appId)
        {
            var appIds = new HashSet<string>();
            appIds.UnionWith(_apps.Values
                .Where(config => config.AppIdentity.Id == appId)
                .Select(config => config.AppIdentity.Version.ToString()));
            return appIds;
        }

        public IEnumerable<string> ListVersions(string appId, string clusterId)
        {
            var appIds = new HashSet<string>();
            appIds.UnionWith(_apps.Values
                .Where(config => config.AppIdentity.Id == appId && config.TargetClusters.Contains(clusterId))
                .Select(config => config.AppIdentity.Version.ToString()));
            return appIds;
        }

        public IEnumerable<string> ListClusters(string appId)
        {
            var clusterIds = new HashSet<string>();
            clusterIds.UnionWith(_apps.Values.Where(config => config.AppIdentity.Id == appId).SelectMany(config => config.TargetClusters));
            return clusterIds;
        }

        public IEnumerable<string> ListClusters(AppIdentity appIdentity)
        {
            AppDeploymentConfig config;
            if (!_apps.TryGetValue(appIdentity, out config))
            {
                return Enumerable.Empty<string>();
            }
            return config.TargetClusters;
        }

        public DeploymentConfig AddApplication(AppIdentity appIdentity, string clusterId)
        {
            AppDeploymentConfig appDeploymentConfig;
            DeploymentConfig deploymentConfig = this;
            if (deploymentConfig.HasApplication(appIdentity))
            {
                appDeploymentConfig = deploymentConfig.GetApplicationConfig(appIdentity);
                appDeploymentConfig = appDeploymentConfig.AddClusterId(clusterId);
                deploymentConfig = RemoveApplication(appIdentity);
            }
            else
            {
                appDeploymentConfig = new AppDeploymentConfig(appIdentity, new [] {clusterId});
            }
            return deploymentConfig.SetApplicationConfig(appDeploymentConfig);
        }

        public DeploymentConfig SetApplicationConfig(AppDeploymentConfig appDeploymentConfig)
        {
            var apps = CopyApps();
            apps[appDeploymentConfig.AppIdentity] = appDeploymentConfig;
            return new DeploymentConfig(apps);
        }

        private Dictionary<AppIdentity, AppDeploymentConfig> CopyApps()
        {
            var apps = _apps.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return apps;
        }

        public AppDeploymentConfig GetApplicationConfig(AppIdentity appIdentity)
        {
            AppDeploymentConfig config;
            if (!_apps.TryGetValue(appIdentity, out config))
            {
                throw new InvalidOperationException($"App {appIdentity} doesn't exist");
            }
            return config;
        }

        public bool HasApplication(string appId)
        {
            return _apps.Keys.Any(identity => identity.Id == appId);
        }

        public bool HasApplication(AppIdentity appIdentity)
        {
            return _apps.ContainsKey(appIdentity);
        }

        public bool HasApplication(AppIdentity appIdentity, string clusterId)
        {
            AppDeploymentConfig config;
            if (!_apps.TryGetValue(appIdentity, out config))
            {
                return false;
            }
            return config.TargetClusters.Contains(clusterId);
        }

        public DeploymentConfig RemoveApplication(string appId)
        {
            if (!HasApplication(appId))
            {
                throw new InvalidOperationException("Cannot remove an application that is not there");
            }
            var apps = CopyApps();
            foreach (AppIdentity appIdentity in _apps.Keys.Where(identity => identity.Id == appId))
            {
                apps.Remove(appIdentity);
            }
            return new DeploymentConfig(apps);
        }
        
        public DeploymentConfig RemoveApplication(AppIdentity appIdentity)
        {
            if (!_apps.ContainsKey(appIdentity))
            {
                throw new InvalidOperationException("Cannot remove an application that is not there");
            }
            var apps = CopyApps();
            apps.Remove(appIdentity);
            return new DeploymentConfig(apps);
        }

        public DeploymentConfig RemoveApplication(AppIdentity appIdentity, string clusterId)
        {
            AppDeploymentConfig config;
            if (!_apps.TryGetValue(appIdentity, out config))
            {
                throw new InvalidOperationException("Cannot remove an application that is not there");
            }
            config = config.RemoveClusterId(clusterId);

            var apps = CopyApps();
            apps.Remove(appIdentity);
            if (config.TargetClusters.Any())
            {
                apps[appIdentity] = config;
            }
            return new DeploymentConfig(apps);
        }

        public IEnumerator<AppDeploymentConfig> GetEnumerator()
        {
            return _apps.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}