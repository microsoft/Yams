using System;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Etg.Yams.Azure.UpdateSession.Retry
{
    public class LockUpdateBlobErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        public bool IsTransient(Exception ex)
        {
            return ex is UpdateBlobUnavailableException;
        }
    }
}