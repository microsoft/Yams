using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Etg.Yams.Application;
using Etg.Yams.Utils;

namespace Etg.Yams.Storage.Config
{
    public class DeploymentConfig
    {
        private readonly IReadOnlyDictionary<string, AppDeploymentConfig> _apps =
            new Dictionary<string, AppDeploymentConfig>();

        public DeploymentConfig()
        {
        }

        private DeploymentConfig(IDictionary<string, AppDeploymentConfig> apps)
        {
            _apps = new ReadOnlyDictionary<string, AppDeploymentConfig>(apps);
        }

        public DeploymentConfig(string rawData)
        {
            _apps = ParseDeploymentsConfig(rawData);
        }

        public IEnumerable<string> ListApplications()
        {
            return _apps.Keys;
        }

        public IEnumerable<string> ListApplications(string deploymentId)
        {
            var apps = new List<string>();
            foreach (string appId in _apps.Keys)
            {
                foreach (KeyValuePair<string, VersionDeploymentConfig> version in _apps[appId].Versions)
                {
                    if (version.Value.DeploymentIds.Contains(deploymentId))
                    {
                        apps.Add(appId);
                    }
                }
            }

            return apps;
        }

        public IEnumerable<string> ListVersions(string appId)
        {
            if (!_apps.ContainsKey(appId))
            {
                return new string[] {};
            }
            return _apps[appId].Versions.Keys;
        }

        public IEnumerable<string> ListVersions(string appId, string deploymentId)
        {
            if (!_apps.ContainsKey(appId))
            {
                return new string[] {};
            }

            return
                _apps[appId].Versions.Where(versionConfig => versionConfig.Value.DeploymentIds.Contains(deploymentId))
                    .Select(versionConfig => versionConfig.Key);
        }

        public IEnumerable<string> ListDeploymentIds(string appId)
        {
            if (!_apps.ContainsKey(appId))
            {
                return new string[] {};
            }
            return _apps[appId].Versions.Values.SelectMany(vc => vc.DeploymentIds);
        }

        public IEnumerable<string> ListDeploymentIds(AppIdentity appIdentity)
        {
            string appId = appIdentity.Id;
            string version = appIdentity.Version.ToString();
            if (!_apps.ContainsKey(appId))
            {
                return new string[] {};
            }
            var appConfig = _apps[appId];
            if (!appConfig.Versions.ContainsKey(version))
            {
                return new string[] {};
            }
            return _apps[appId].Versions[version].DeploymentIds;
        }

        public DeploymentConfig AddApplication(AppIdentity appIdentity, string deploymentId)
        {
            string appId = appIdentity.Id;
            string version = appIdentity.Version.ToString();
            Dictionary<string, AppDeploymentConfig> apps = GetAppsCopy();
            apps = AddAppConfigIfNoneExists(apps, appId);
            AppDeploymentConfig appDeploymentConfig = apps[appId];
            appDeploymentConfig = AddVersionConfigIfNoneExists(appDeploymentConfig, version);
            VersionDeploymentConfig versionDeploymentConfig = appDeploymentConfig.Versions[version];
            if (versionDeploymentConfig.DeploymentIds.Contains(deploymentId))
            {
                throw new InvalidOperationException(
                    $"Cannot add the deployment {deploymentId} to application {appId}, version {version} because it's already there");
            }
            versionDeploymentConfig = versionDeploymentConfig.AddDeployment(deploymentId);
            appDeploymentConfig = appDeploymentConfig.SetVersionConfig(versionDeploymentConfig);
            apps[appId] = appDeploymentConfig;
            return new DeploymentConfig(apps);
        }

        private Dictionary<string, AppDeploymentConfig> GetAppsCopy()
        {
            return new Dictionary<string, AppDeploymentConfig>(DictionaryUtils.ToDictionary(_apps));
        }

        public bool HasApplication(string appId)
        {
            return _apps.ContainsKey(appId);
        }

        public bool HasApplication(AppIdentity appIdentity)
        {
            string appId = appIdentity.Id;
            string version = appIdentity.Version.ToString();
            if (!HasApplication(appId))
            {
                return false;
            }
            return _apps[appId].Versions.ContainsKey(version);
        }

        public bool HasApplication(AppIdentity appIdentity, string deploymentId)
        {
            string appId = appIdentity.Id;
            string version = appIdentity.Version.ToString();
            if (!HasApplication(appIdentity))
            {
                return false;
            }
            return _apps[appId].Versions[version].DeploymentIds.Contains(deploymentId);
        }

        public DeploymentConfig RemoveApplication(string appId)
        {
            var apps = GetAppsCopy();
            if (!apps.ContainsKey(appId))
            {
                throw new InvalidOperationException($"Cannot remove application {appId} because it was not found");
            }
            apps.Remove(appId);
            return new DeploymentConfig(apps);
        }

        public DeploymentConfig RemoveApplication(AppIdentity appIdentity)
        {
            string appId = appIdentity.Id;
            string version = appIdentity.Version.ToString();
            var apps = GetAppsCopy();
            if (!apps.ContainsKey(appId))
            {
                throw new InvalidOperationException($"Cannot remove version {version} because {appId} was not found");
            }
            AppDeploymentConfig appDeploymentConfig = apps[appId];
            if (!appDeploymentConfig.Versions.ContainsKey(version))
            {
                throw new InvalidOperationException(
                    $"Cannot remove version {version} from application {appId} because version {version} was not found");
            }
            appDeploymentConfig = appDeploymentConfig.RemoveVersionConfig(version);
            if (!appDeploymentConfig.Versions.Any())
            {
                apps.Remove(appId);
            }
            else
            {
                apps[appId] = appDeploymentConfig;
            }

            return new DeploymentConfig(apps);
        }

        public DeploymentConfig RemoveApplication(AppIdentity appIdentity, string deploymentId)
        {
            string appId = appIdentity.Id;
            string version = appIdentity.Version.ToString();
            var apps = GetAppsCopy();
            if (!apps.ContainsKey(appId))
            {
                throw new InvalidOperationException($"Cannot remove deployment because app {appId} was not found");
            }
            AppDeploymentConfig appDeploymentConfig = apps[appId];
            if (!appDeploymentConfig.Versions.ContainsKey(version))
            {
                throw new InvalidOperationException(
                    $"Cannot remove deployment because app {appId}, version {version} was not found");
            }
            VersionDeploymentConfig versionDeploymentConfig = appDeploymentConfig.Versions[version];
            if (!versionDeploymentConfig.DeploymentIds.Contains(deploymentId))
            {
                throw new InvalidOperationException(
                    $"Cannot remove deployment {deploymentId} from app {appId}, version {version} because it was not found");
            }
            versionDeploymentConfig = versionDeploymentConfig.RemoveDeployment(deploymentId);
            appDeploymentConfig = appDeploymentConfig.SetVersionConfig(versionDeploymentConfig);
            apps[appId] = appDeploymentConfig;
            DeploymentConfig dc = new DeploymentConfig(apps);

            if (!dc._apps[appId].Versions[version].DeploymentIds.Any())
            {
                dc = dc.RemoveApplication(appIdentity);
            }
            return dc;
        }

        public string RawData()
        {
            var applicationsList = new List<ApplicationData>();
            foreach (KeyValuePair<string, AppDeploymentConfig> app in _apps)
            {
                string appId = app.Key;
                AppDeploymentConfig appDeploymentConfig = app.Value;
                foreach (KeyValuePair<string, VersionDeploymentConfig> v in appDeploymentConfig.Versions)
                {
                    string version = v.Key;
                    VersionDeploymentConfig versionDeploymentConfig = v.Value;
                    applicationsList.Add(new ApplicationData(appId, version,
                        versionDeploymentConfig.DeploymentIds.ToArray()));
                }
            }
            ApplicationsData applicationsData = new ApplicationsData(applicationsList.ToArray());
            return JsonUtils.Serialize(applicationsData);
        }

        private AppDeploymentConfig AddVersionConfigIfNoneExists(AppDeploymentConfig appDeploymentConfig, string version)
        {
            if (!appDeploymentConfig.Versions.ContainsKey(version))
            {
                return appDeploymentConfig.SetVersionConfig(new VersionDeploymentConfig(version));
            }
            return appDeploymentConfig;
        }

        private static Dictionary<string, AppDeploymentConfig> AddAppConfigIfNoneExists(
            Dictionary<string, AppDeploymentConfig> apps, string appId)
        {
            if (!apps.ContainsKey(appId))
            {
                var appConfig = new AppDeploymentConfig(appId);
                apps[appId] = appConfig;
            }
            return apps;
        }

        private static Dictionary<string, AppDeploymentConfig> ParseDeploymentsConfig(string json)
        {
            Dictionary<string, AppDeploymentConfig> apps = new Dictionary<string, AppDeploymentConfig>();
            if (string.IsNullOrEmpty(json))
            {
                return apps;
            }

            ApplicationsData appsData = JsonUtils.Deserialize<ApplicationsData>(json);
            foreach (ApplicationData appData in appsData.Applications)
            {
                VersionDeploymentConfig versionDeploymentConfig = new VersionDeploymentConfig(appData.Version,
                    appData.DeploymentIds);
                apps = AddAppConfigIfNoneExists(apps, appData.Id);
                AppDeploymentConfig appDeploymentConfig = apps[appData.Id];
                appDeploymentConfig = appDeploymentConfig.SetVersionConfig(versionDeploymentConfig);
                apps[appData.Id] = appDeploymentConfig;
            }
            return apps;
        }

        private class ApplicationData
        {
            public ApplicationData(string id, string version, string[] deploymentIds)
            {
                Id = id;
                Version = version;
                DeploymentIds = deploymentIds;
            }

            public string Id { get; private set; }
            public string Version { get; private set; }
            public string[] DeploymentIds { get; private set; }
        }

        private class ApplicationsData
        {
            public ApplicationsData(ApplicationData[] applications)
            {
                Applications = applications;
            }

            public ApplicationData[] Applications { get; private set; }
        }
    }
}