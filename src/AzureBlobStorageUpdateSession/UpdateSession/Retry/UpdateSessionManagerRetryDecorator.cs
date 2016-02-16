using System.Threading.Tasks;
using Etg.Yams.Update;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Etg.Yams.Azure.UpdateSession.Retry
{
    public class UpdateSessionManagerRetryDecorator : IUpdateSessionManager
    {
        private readonly IUpdateSessionManager _updateSessionManager;
        private readonly RetryPolicy _retryPolicy;

        public UpdateSessionManagerRetryDecorator(IUpdateSessionManager updateSessionManager,
            RetryStrategy retryStrategy,
            ITransientErrorDetectionStrategy errorDetectionStrategy)
        {
            _updateSessionManager = updateSessionManager;
            _retryPolicy = new RetryPolicy(errorDetectionStrategy, retryStrategy);
        }

        public Task<bool> TryStartUpdateSession(string applicationId)
        {
            return _retryPolicy.ExecuteAsync(async () => await _updateSessionManager.TryStartUpdateSession(applicationId));
        }

        public Task EndUpdateSession(string applicationId)
        {
            return _retryPolicy.ExecuteAsync(async () => await _updateSessionManager.EndUpdateSession(applicationId));
        }
    }
}