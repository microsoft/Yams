using System.Threading.Tasks;
using Etg.Yams.Storage.Status;

namespace Etg.Yams.Storage
{
    public interface IDeploymentStatusReader
    {
        Task<InstanceDeploymentStatus> FetchInstanceDeploymentStatus(string clusterId, string instanceId);
    }
}