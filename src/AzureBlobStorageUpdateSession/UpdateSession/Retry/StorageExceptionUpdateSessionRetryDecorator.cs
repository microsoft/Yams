using System.Threading.Tasks;
using Etg.Yams.Update;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Etg.Yams.Azure.UpdateSession.Retry
{
    public class StorageExceptionUpdateSessionRetryDecorator : IUpdateSessionManager
    {
        private readonly IUpdateSessionManager _updateSessionManager;
        private readonly RetryPolicy _retryPolicy;

        public StorageExceptionUpdateSessionRetryDecorator(IUpdateSessionManager updateSessionManager,
            RetryStrategy retryStrategy,
            ITransientErrorDetectionStrategy errorDetectionStrategy)
        {
            _updateSessionManager = updateSessionManager;
            _retryPolicy = new RetryPolicy(errorDetectionStrategy, retryStrategy);
        }

        public Task<bool> TryStartUpdateSession()
        {
            return _retryPolicy.ExecuteAsync(async () => await _updateSessionManager.TryStartUpdateSession());
        }

        public Task EndUpdateSession()
        {
            return _retryPolicy.ExecuteAsync(async () => await _updateSessionManager.EndUpdateSession());
        }
    }
}