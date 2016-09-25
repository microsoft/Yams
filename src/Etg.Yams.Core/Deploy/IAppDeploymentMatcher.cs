using Etg.Yams.Storage.Config;

namespace Etg.Yams.Deploy
{
    public interface IAppDeploymentMatcher
    {
        /// <summary>
        /// This interface is used to determine if an app should be deployed to the current cluster or not.
        /// </summary>
        /// <param name="appDeploymentConfig"></param>
        /// <returns>True if the app corresponding to the given AppDeploymentConfig should be deployed to the current cluster, 
        /// false otherwise</returns>
        bool IsMatch(AppDeploymentConfig appDeploymentConfig);
    }
}