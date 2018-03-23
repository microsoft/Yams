using System.Threading.Tasks;

namespace Etg.Yams.Azure.UpdateSession
{
    public interface IUpdateSessionTable
    {
        Task<UpdateSessionStatus> FetchUpdateSessionStatus(string clusterId);
        Task<bool> TryExecuteTransaction(UpdateSessionTransaction transaction);
        Task DeleteInstanceEntity(string clusterId, string instanceId);
        Task<string> GetActiveUpdateDomain(string clusterId);
    }
}