using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Lease
{
    public class BlobLeaseFactory : IBlobLeaseFactory
    {
        private readonly int _renewIntervalInSeconds;

        public BlobLeaseFactory(int renewIntervalInSeconds)
        {
            _renewIntervalInSeconds = renewIntervalInSeconds;
        }

        public IBlobLease CreateLease(ICloudBlob blob)
        {
            return new SelfRenewableBlobLease(blob, _renewIntervalInSeconds);
        }
    }
}
