using System.Collections.Generic;
using Etg.Yams.Utils;

namespace Etg.Yams.Storage.Config
{
    internal class AppDeploymentConfig
    {
        public AppDeploymentConfig(string appId)
        {
            AppId = appId;
            Versions = new Dictionary<string, VersionDeploymentConfig>();
        }

        public AppDeploymentConfig(string appId, IDictionary<string, VersionDeploymentConfig> versions)
        {
            AppId = appId;
            Versions = new Dictionary<string, VersionDeploymentConfig>(versions);
        }

        public AppDeploymentConfig SetVersionConfig(VersionDeploymentConfig versionDeploymentConfig)
        {
            var versions = new Dictionary<string, VersionDeploymentConfig>(ToDictionary(Versions));
            versions[versionDeploymentConfig.Version] = versionDeploymentConfig;
            return new AppDeploymentConfig(AppId, versions);
        }

        public AppDeploymentConfig RemoveVersionConfig(string version)
        {
            var versions = new Dictionary<string, VersionDeploymentConfig>(ToDictionary(Versions));
            versions.Remove(version);
            return new AppDeploymentConfig(AppId, versions);
        }

        private static Dictionary<string, VersionDeploymentConfig> ToDictionary(
            IReadOnlyDictionary<string, VersionDeploymentConfig> roDict)
        {
            return DictionaryUtils.ToDictionary(roDict);
        }

        public string AppId { get; }

        public IReadOnlyDictionary<string, VersionDeploymentConfig> Versions { get; }
    }
}