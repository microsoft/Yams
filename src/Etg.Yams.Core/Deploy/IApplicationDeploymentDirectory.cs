using System.Collections.Generic;
using System.Threading.Tasks;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Deploy
{
    public interface IApplicationDeploymentDirectory
    {
        /// <returns>the list of application that should be deployed to the current cluster</returns>
        Task<IEnumerable<AppDeploymentConfig>> FetchDeployments();
    }
}