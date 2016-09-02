using Etg.Yams.WorkerRole;

namespace Backend
{
    public class WorkerRole : YamsWorkerRole
    {
        protected override bool IsSingleClusterDeployment
        {
            get { return false; }
        }
    }
}
