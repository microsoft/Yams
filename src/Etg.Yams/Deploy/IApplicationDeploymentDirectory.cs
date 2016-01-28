using System.Collections.Generic;
using System.Threading.Tasks;
using Etg.Yams.Application;

namespace Etg.Yams.Deploy
{
    public interface IApplicationDeploymentDirectory
    {
        /// <summary>
        /// Returns the list of application that should be deployed on the given deploymentId.
        /// </summary>
        /// <param name="deploymentId"></param>
        /// <returns></returns>
        Task<IEnumerable<AppIdentity>> FetchDeployments(string deploymentId);
    }
}