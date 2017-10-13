using System.Threading.Tasks;
using Etg.Yams.Storage.Status;

namespace Etg.Yams.Storage
{
    public interface IDeploymentStatusWriter
    {
        Task PublishInstanceDeploymentStatus(string clusterId, string instanceId,
            InstanceDeploymentStatus instanceDeploymentStatus);
    }
}