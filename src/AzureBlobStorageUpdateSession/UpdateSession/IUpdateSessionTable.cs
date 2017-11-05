using System.Threading.Tasks;

namespace Etg.Yams.Azure.UpdateSession
{
    public interface IUpdateSessionTable
    {
        Task<UpdateSessionStatus> FetchUpdateSessionStatus(string clusterId, string appId);
        Task<bool> TryExecuteTransaction(UpdateSessionTransaction transaction);
        Task DeleteInstanceEntity(string clusterId, string instanceId, string appId);
        Task<string> GetActiveUpdateDomain(string clusterId, string appId);
    }
}