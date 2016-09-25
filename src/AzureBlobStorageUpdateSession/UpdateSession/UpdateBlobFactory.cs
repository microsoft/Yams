using System.Threading.Tasks;
using Etg.Yams.Azure.Lease;
using Etg.Yams.Azure.Utils;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Azure.UpdateSession
{
    public class UpdateBlobFactory : IUpdateBlobFactory
    {
        private readonly string _clusterId;
        private readonly CloudBlobContainer _blobContainer;
        private readonly IBlobLeaseFactory _blobLeaseFactory;

        public UpdateBlobFactory(string clusterId, CloudBlobContainer blobContainer, IBlobLeaseFactory blobLeaseFactory)
        {
            _clusterId = clusterId;
            _blobContainer = blobContainer;
            _blobLeaseFactory = blobLeaseFactory;
        }

        public async Task<IUpdateBlob> TryLockUpdateBlob(string appId)
        {
            string updateBlobName = GetUpdateBlobName(appId);
            ICloudBlob blob = GetBlob(updateBlobName);
            await CreateBlobIfNoneExists(blob);

            UpdateBlob updateBlob = new UpdateBlob(blob, _blobLeaseFactory);
            bool locked = await updateBlob.TryLock();
            if (locked == false)
            {
                throw new UpdateBlobUnavailableException();
            }
            return updateBlob;
        }

        private string GetUpdateBlobName(string applicationId)
        {
            return _clusterId + "_" + applicationId + "_update_blob";
        }

        private CloudBlockBlob GetBlob(string updateBlobName)
        {
            return _blobContainer.GetBlockBlobReference(updateBlobName);
        }

        private static async Task CreateBlobIfNoneExists(ICloudBlob updateBlob)
        {
            if (!await updateBlob.ExistsAsync())
            {
                await BlobUtils.CreateEmptyBlob(updateBlob);
            }
        }
    }
}