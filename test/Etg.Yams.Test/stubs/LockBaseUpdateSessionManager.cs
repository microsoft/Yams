using System.Collections.Generic;
using System.Threading.Tasks;
using Etg.Yams.Update;

namespace Etg.Yams.Test.stubs
{
    public class LockBasedUpdateSessionManager : IUpdateSessionManager
    {
        private readonly ISet<string> _lockedIds;

        public LockBasedUpdateSessionManager()
        {
            _lockedIds = new HashSet<string>();
        }

        public Task<bool> TryStartUpdateSession(string applicationId)
        {
            lock (_lockedIds)
            {
                if (_lockedIds.Contains(applicationId))
                {
                    return Task.FromResult(false);
                }
                _lockedIds.Add(applicationId);
                return Task.FromResult(true);
            }
        }

        public Task EndUpdateSession(string applicationId)
        {
            lock (_lockedIds)
            {
                _lockedIds.Remove(applicationId);
            }
            return Task.FromResult(true);
        }
    }
}
