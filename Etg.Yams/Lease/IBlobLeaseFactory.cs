using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Lease
{
    public interface IBlobLeaseFactory
    {
        IBlobLease CreateLease(ICloudBlob blob);
    }
}
