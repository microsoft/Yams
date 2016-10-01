using System.Threading.Tasks;
using Etg.Yams.Install;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Application
{
    public interface IApplicationConfigParser
    {
        /// <summary>
        /// Parses the application config file.
        /// </summary>
        /// <param name="path">The absolute path of the application config file</param>
        /// <param name="appInstallConfig">The installation config of the corresponding app</param>
        /// <returns></returns>
        Task<ApplicationConfig> ParseFile(string path, AppInstallConfig appInstallConfig);
    }
}