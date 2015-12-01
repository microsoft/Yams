using System.Threading.Tasks;

namespace Etg.Yams.Application
{
    public interface IApplicationConfigSymbolResolver
    {
        /// <summary>
        /// Resolves the given symbol for the given app.
        /// Symbols are used in the AppConfig.json file with the syntax ${symbol}.
        /// </summary>
        /// <param name="appIdentity">The identity of the app involved</param>
        /// <param name="symbol">The symbol to resolve WITHOUT the $ and brackets</param>
        /// <returns>The value of the symbol</returns>
        Task<string> ResolveSymbol(AppIdentity appIdentity, string symbol);
    }
}
