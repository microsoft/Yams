using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Azure.Lease
{
    public class BlobLeaseFactory : IBlobLeaseFactory
    {
        public IBlobLease CreateLease(ICloudBlob blob)
        {
            return new SelfRenewableBlobLease(blob);
        }
    }
}
