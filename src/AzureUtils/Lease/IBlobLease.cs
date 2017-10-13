using System;
using System.Threading.Tasks;

namespace Etg.Yams.Azure.Lease
{
    public interface IBlobLease : IDisposable
    {
        /// <summary>
        /// </summary>
        /// <returns>The leaseId if the lease is successfully acquired, null otherwise</returns>
        Task<string> TryAcquireLease();

        Task ReleaseLease();
    }
}