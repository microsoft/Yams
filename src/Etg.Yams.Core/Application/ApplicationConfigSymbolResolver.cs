using System.Collections.Generic;
using System.Threading.Tasks;
using Etg.Yams.Install;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Application
{
    public class ApplicationConfigSymbolResolver : IApplicationConfigSymbolResolver
    {
        private readonly string _instanceId;
        private readonly IReadOnlyDictionary<string, string> _clusterProperties;
        private readonly string _clusterId;

        public ApplicationConfigSymbolResolver(string clusterId, string instanceId) : this(clusterId, instanceId, new Dictionary<string, string>())
        {
        }

        public ApplicationConfigSymbolResolver(string clusterId, string instanceId, IReadOnlyDictionary<string, string> clusterProperties)
        {
            _instanceId = instanceId;
            _clusterProperties = clusterProperties;
            _clusterId = clusterId;
        }

        public Task<string> ResolveSymbol(AppInstallConfig appInstallConfig, string symbol)
        {
            string symbolValue = symbol;
            AppIdentity appIdentity = appInstallConfig.AppIdentity;
            switch (symbol)
            {
                case "Id":
                    symbolValue = appIdentity.Id;
                    break;
                case "Version":
                    symbolValue = appIdentity.Version.ToString();
                    break;
                case "Version.Major":
                    symbolValue = appIdentity.Version.Major.ToString();
                    break;
                case "Version.Minor":
                    symbolValue = appIdentity.Version.Minor.ToString();
                    break;
                case "Version.Build":
                    symbolValue = appIdentity.Version.Patch.ToString();
                    break;
                case "Version.Prerelease":
                    symbolValue = appIdentity.Version.Prerelease;
                    break;
                case "ClusterId":
                    symbolValue = _clusterId;
                    break;
                // TODO: This has been kept for backward compatibility; remove at some point
                case "DeploymentId":
                    symbolValue = _clusterId;
                    break;
                case "InstanceId":
                    symbolValue = _instanceId;
                    break;
                default:
                    if (!appInstallConfig.Properties.TryGetValue(symbol, out symbolValue))
                    {
                        _clusterProperties.TryGetValue(symbol, out symbolValue);
                    }
                    break;
            }
            return Task.FromResult(symbolValue);
        }
    }
}