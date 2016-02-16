using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Etg.Yams.Azure.UpdateSession.Retry
{
    public class UpdateBlobFactoryRetryLockDecorator : IUpdateBlobFactory
    {
        private readonly IUpdateBlobFactory _updateBlobFactory;
        private readonly RetryPolicy _retryPolicy;

        public UpdateBlobFactoryRetryLockDecorator(IUpdateBlobFactory updateBlobFactory, RetryStrategy retryStrategy)
        {
            _updateBlobFactory = updateBlobFactory;
            _retryPolicy = new RetryPolicy(new LockUpdateBlobErrorDetectionStrategy(), retryStrategy);
        }

        public Task<IUpdateBlob> TryLockUpdateBlob(string appId)
        {
            return _retryPolicy.ExecuteAsync(() => _updateBlobFactory.TryLockUpdateBlob(appId));
        }
    }
}