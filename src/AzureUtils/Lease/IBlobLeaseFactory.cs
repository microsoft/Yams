using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Azure.Lease
{
    public interface IBlobLeaseFactory
    {
        IBlobLease CreateLease(ICloudBlob blob);
    }
}
