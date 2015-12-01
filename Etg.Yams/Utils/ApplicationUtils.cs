using System.IO;
using Etg.Yams.Application;

namespace Etg.Yams.Utils
{
    public static class ApplicationUtils
    {
        public static string GetApplicationRelativePath(AppIdentity appIdentity)
        {
            return Path.Combine(appIdentity.Id, appIdentity.Version.ToString());
        }
    }
}