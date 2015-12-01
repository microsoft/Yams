using System.Threading.Tasks;

namespace Etg.Yams.Lease
{
    public interface IBlobLease
    {
        /// <summary>
        /// </summary>
        /// <returns>The leaseId if the lease is successfully acquired, null otherwise</returns>
        Task<string> TryAcquireLease();

        Task ReleaseLease();
    }
}