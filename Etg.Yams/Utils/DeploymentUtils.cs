using System.IO;
using Etg.Yams.Application;

namespace Etg.Yams.Utils
{
    public static class DeploymentUtils
    {
        public static string GetDeploymentRelativePath(AppIdentity appIdentity)
        {
            return Path.Combine(appIdentity.Id, appIdentity.Version.ToString());
        }
    }
}
