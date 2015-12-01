using System.Threading.Tasks;

namespace Etg.Yams.Application
{
    public interface IApplicationConfigParser
    {
        /// <summary>
        /// Parses the application config file.
        /// </summary>
        /// <param name="path">The absolute path of the application config file</param>
        /// <param name="identity">The identity of the corresponding app</param>
        /// <returns></returns>
        Task<ApplicationConfig> ParseFile(string path, AppIdentity identity);
    }
}