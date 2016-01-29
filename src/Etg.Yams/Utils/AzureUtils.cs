using Microsoft.WindowsAzure.ServiceRuntime;

namespace Etg.Yams.Utils
{
    public static class AzureUtils
    {
        public static bool IsEmulator()
        {
            return RoleEnvironment.IsAvailable && RoleEnvironment.IsEmulated;
        }
    }
}
