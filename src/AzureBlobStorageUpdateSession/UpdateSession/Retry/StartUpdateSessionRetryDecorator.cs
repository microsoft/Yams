using System;
using System.Threading.Tasks;
using Etg.Yams.Update;

namespace Etg.Yams.Azure.UpdateSession.Retry
{
    public class StartUpdateSessionRetryDecorator : IUpdateSessionManager
    {
        private readonly IUpdateSessionManager _updateSessionManager;
        private readonly int _retryCount;
        private readonly TimeSpan _retryInterval;

        public StartUpdateSessionRetryDecorator(IUpdateSessionManager updateSessionManager, int retryCount,
            TimeSpan retryInterval)
        {
            _updateSessionManager = updateSessionManager;
            _retryCount = retryCount;
            _retryInterval = retryInterval;
        }

        public async Task<bool> TryStartUpdateSession(string applicationId)
        {
            int count = 0;
            while (count <= _retryCount)
            {
                if (await _updateSessionManager.TryStartUpdateSession(applicationId))
                {
                    return true;
                }

                ++count;
                if (count <= _retryCount)
                {
                    await Task.Delay(_retryInterval);
                }
            }
            return false;
        }

        public Task EndUpdateSession(string applicationId)
        {
            return _updateSessionManager.EndUpdateSession(applicationId);
        }
    }
}