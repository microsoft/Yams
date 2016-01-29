using Microsoft.WindowsAzure.ServiceRuntime;

namespace Etg.Yams.WorkerRole.Utils
{
    public static class DeploymentIdUtils
    {
        public static string CloudServiceDeploymentId
        {
            get
            {
                if (!RoleEnvironment.IsAvailable || RoleEnvironment.IsEmulated)
                {
                    return Constants.TestDeploymentId;
                }
                return RoleEnvironment.DeploymentId;
            }
        }
    }
}
