using System.Threading.Tasks;
using Etg.Yams.Install;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Application
{
    public interface IApplicationConfigSymbolResolver
    {
        /// <summary>
        /// Resolves the given symbol for the given app.
        /// Symbols are used in the AppConfig.json file with the syntax ${symbol}.
        /// </summary>
        /// <param name="appDeploymentConfig">The deployment configuration of the app involved</param>
        /// <param name="symbol">The symbol to resolve WITHOUT the $ and brackets</param>
        /// <returns>The value of the symbol</returns>
        Task<string> ResolveSymbol(AppInstallConfig appInstallConfig, string symbol);
    }
}
