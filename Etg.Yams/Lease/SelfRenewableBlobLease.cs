using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Lease
{
    /// <summary>
    /// A lease that doesn't expire.
    /// TODO: Investigate if an alternative exists in Azure and get rid of this class if possible.
    /// </summary>
    public class SelfRenewableBlobLease : IBlobLease
    {
        private string _leaseId;
        private readonly int _renewIntervalInSeconds;
        private readonly ICloudBlob _blob;

        private IDisposable KeepAliveRxTimer { get; set; }

        public SelfRenewableBlobLease(ICloudBlob blob, int renewIntervalInSeconds)
        {
            if (renewIntervalInSeconds > 60)
            {
                throw new Exception("Blob lease renew interval must be less than 60s");
            }
            if (renewIntervalInSeconds < 10)
            {
                throw new Exception("Blob lease renew interval must be at least 10s");
            }
            _renewIntervalInSeconds = renewIntervalInSeconds;
            _blob = blob;
        }

        public async Task<string> TryAcquireLease()
        {
            try
            {
                var leaseTime = TimeSpan.FromSeconds(_renewIntervalInSeconds);
                _leaseId = await _blob.AcquireLeaseAsync(leaseTime, null);
                KeepAliveRxTimer = Observable.Interval(TimeSpan.FromSeconds(_renewIntervalInSeconds - 9.0)).Subscribe(async l => await RenewLease(_blob, _leaseId));
            }
            catch (StorageException ex)
            {
                _leaseId = null;
                Trace.TraceInformation(string.Format("Failed to acquire the lease on blob {0}", _blob), ex);
            }
            if (_leaseId == null)
            {
                await DisableTimer();
            }
            return _leaseId;
        }

        private Task DisableTimer()
        {
            return Task.Run(() =>
            {
                if (KeepAliveRxTimer != null)
                {
                    KeepAliveRxTimer.Dispose();
                }
            });
        }


        public async Task ReleaseLease()
        {
            await DisableTimer();
            if (_leaseId == null)
            {
                Trace.TraceWarning("Attempt to release a blob that is not locked.. will ignore");
                return;
            }
            
            try
            {
                await _blob.ReleaseLeaseAsync(
                    new AccessCondition
                    {
                        LeaseId = _leaseId
                    });
                _leaseId = null;
            }
            catch (StorageException ex)
            {
                Trace.TraceError("Failed to release lease on blob {0} using lease id {1}. Exception was: {2}", _blob.Name, _leaseId, ex);
            }
        }

        private static async Task RenewLease(ICloudBlob blob, string leaseId)
        {
            var blobKey = GetKey(blob);
            try
            {
                await blob.RenewLeaseAsync(
                    new AccessCondition
                    {
                        LeaseId = leaseId
                    });
                Trace.TraceInformation("Renewed lease for blob {0}", blobKey);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to renew lease for blob {0}. Exception was: {1}", blobKey, ex);
            }
        }

        private static string GetKey(ICloudBlob blob)
        {
            return blob.Container.Name + "-" + blob.Name;
        }
    }
}