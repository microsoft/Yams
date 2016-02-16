using System;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Microsoft.WindowsAzure.Storage;

namespace Etg.Yams.Azure.UpdateSession.Retry
{
    public class StorageExceptionErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        public bool IsTransient(Exception ex)
        {
            return ex is StorageException;
        }
    }
}