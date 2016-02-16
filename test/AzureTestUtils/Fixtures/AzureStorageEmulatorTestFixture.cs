using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.AzureTestUtils.Fixtures
{
    public class AzureStorageEmulatorTestFixture
    {
        public CloudBlobClient BlobClient { get; }
        private readonly AzureStorageEmulatorProxy _azureStorageEmulatorProxy;
        public AzureStorageEmulatorTestFixture()
        {
            var account = CloudStorageAccount.DevelopmentStorageAccount;
            BlobClient = account.CreateCloudBlobClient();

            _azureStorageEmulatorProxy = new AzureStorageEmulatorProxy();
            _azureStorageEmulatorProxy.StartEmulator();
        }

        public void ClearBlobStorage()
        {
            _azureStorageEmulatorProxy.ClearBlobStorage();
        }

        public void Dispose()
        {
            _azureStorageEmulatorProxy.StopEmulator();
        }
    }
}