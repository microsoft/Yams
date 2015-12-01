using System.Collections.Generic;

namespace Etg.Yams.Deploy
{
    /// <summary>
    /// Deployment configuration object for all applications.
    /// </summary>
    public class DeploymentDirectoryConfig
    {
        public readonly IEnumerable<DeploymentConfig> DeploymentsConfigs;

        public DeploymentDirectoryConfig(IEnumerable<DeploymentConfig> deploymentsConfigs)
        {
            DeploymentsConfigs = deploymentsConfigs;
        }
    }
}
