using AzureTestUtils;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Test.Fixtures
{
    public class StorageEmulatorTestFixture
    {
        public CloudBlobClient BlobClient { get; }
        private readonly StorageEmulatorProxy _storageEmulatorProxy;
        public StorageEmulatorTestFixture()
        {
            var account = CloudStorageAccount.DevelopmentStorageAccount;
            BlobClient = account.CreateCloudBlobClient();

            _storageEmulatorProxy = new StorageEmulatorProxy();
            _storageEmulatorProxy.StartEmulator();
        }

        public void ClearBlobStorage()
        {
            _storageEmulatorProxy.ClearBlobStorage();
        }

        public void Dispose()
        {
            _storageEmulatorProxy.StopEmulator();
        }
    }
}