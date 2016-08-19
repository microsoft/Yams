using System;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Etg.Yams.WorkerRole.Utils
{
    public static class DeploymentIdUtils
    {
        public static string CloudServiceRoleIdentifier
        {
            get
            {
                if (!RoleEnvironment.IsAvailable)
                {
                    return Constants.TestDeploymentId;
                }

                string deploymentId = RoleEnvironment.IsEmulated
                    ? Constants.TestDeploymentId
                    : RoleEnvironment.DeploymentId;

                // This concatenates the Cloud Service deployment id and the role name so that there is a unique name for each role in the Cloud Service
                return $"{deploymentId}_{RoleEnvironment.CurrentRoleInstance.Role.Name}";
            }
        }
    }
}
