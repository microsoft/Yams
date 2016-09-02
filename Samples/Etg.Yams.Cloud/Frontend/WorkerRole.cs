using Etg.Yams.WorkerRole;

namespace Frontend
{
    public class WorkerRole : YamsWorkerRole
    {
        protected override bool IsSingleClusterDeployment
        {
            get { return false; }
        }
    }
}
