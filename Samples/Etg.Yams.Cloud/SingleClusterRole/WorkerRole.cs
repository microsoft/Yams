using Etg.Yams.WorkerRole;

namespace SingleClusterRole
{
    public class WorkerRole : YamsWorkerRole
    {
        protected override bool IsSingleClusterDeployment
        {
            get { return true; }
        }
    }
}
