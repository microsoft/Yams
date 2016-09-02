using System.Threading.Tasks;

namespace Etg.Yams.Application
{
    public class ApplicationConfigSymbolResolver : IApplicationConfigSymbolResolver
    {
        private readonly string _instanceId;
        private readonly string _deploymentId;

        public ApplicationConfigSymbolResolver(string deploymentId, string instanceId)
        {
            _instanceId = instanceId;
            _deploymentId = deploymentId;
        }

        public Task<string> ResolveSymbol(AppIdentity appIdentity, string symbol)
        {
            string symbolValue = symbol;
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
                    symbolValue = appIdentity.Version.Build;
                    break;
                case "Version.Prerelease":
                    symbolValue = appIdentity.Version.Prerelease;
                    break;
                case "DeploymentId":
                    symbolValue = _deploymentId;
                    break;
                case "InstanceId":
                    symbolValue = _instanceId;
                    break;
            }
            return Task.FromResult(symbolValue);
        }
    }
}