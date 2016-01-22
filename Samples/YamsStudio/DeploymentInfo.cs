using Etg.Yams.Application;

namespace YamsStudio
{
    public class DeploymentInfo
    {
	    public DeploymentInfo(AppIdentity appIdentity, string deploymentId)
        {
            AppIdentity = appIdentity;
            DeploymentId = deploymentId;
        }

        public AppIdentity AppIdentity { get; }

	    public string DeploymentId { get; }
    }
}