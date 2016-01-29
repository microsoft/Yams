using Microsoft.WindowsAzure.ServiceRuntime;

namespace Etg.Yams.WorkerRole.Utils
{
    public static class AzureUtils
    {
        public static bool IsEmulator()
        {
            return RoleEnvironment.IsAvailable && RoleEnvironment.IsEmulated;
        }
    }
}
