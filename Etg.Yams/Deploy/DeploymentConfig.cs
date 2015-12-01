using System.Collections.Generic;
using Etg.Yams.Application;

namespace Etg.Yams.Deploy
{
    /// <summary>
    /// Configuration object for a deployment of an application
    /// </summary>
    public class DeploymentConfig
    {
        private readonly AppIdentity _appIdentity;
        private readonly ISet<string> _deploymentIds;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appIdentity">The identity of the application</param>
        /// <param name="deploymentIds"> The list of deployment ids where the application should be deployed</param>
        public DeploymentConfig(AppIdentity appIdentity, IEnumerable<string> deploymentIds)
        {
            _appIdentity = appIdentity;
            _deploymentIds = new HashSet<string>(deploymentIds);
        }

        public AppIdentity AppIdentity
        {
            get { return _appIdentity; }
        }

        /// <summary>
        /// The list of deployment ids where the application should be deployed
        /// </summary>
        public ISet<string> DeploymentIds
        {
            get { return _deploymentIds; }
        }
    }
}
