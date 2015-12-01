using System.IO;
using System.Threading.Tasks;
using AzureTestUtils;
using Etg.Yams.IO;
using Etg.Yams.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Test.IO
{
    [TestClass]
    public class AzureBlobTest
    {
        private static CloudStorageAccount _account;
        private static CloudBlobClient _blobClient;
        private static StorageEmulatorProxy _storageEmulatorProxy;

        [ClassInitialize]
        public static void StartAndCleanStorage(TestContext cont)
        {
            _account = CloudStorageAccount.DevelopmentStorageAccount;
            _blobClient = _account.CreateCloudBlobClient();

            _storageEmulatorProxy = new StorageEmulatorProxy();
            _storageEmulatorProxy.StartEmulator();
        }

        [ClassCleanup]
        public static void ShutdownStorage()
        {
            _storageEmulatorProxy.StopEmulator();
        }

        [TestInitialize]
        public void CleanAndRestartStorage()
        {
            _storageEmulatorProxy.ClearBlobStorage();
        }

        [TestMethod]
        public async Task TestName()
        {
            string blobName = "blob";
            CloudBlockBlob blob = await CreateEmptyBlob(blobName);
            AzureBlob azureBlob = new AzureBlob(blob);

            Assert.AreEqual(blobName, azureBlob.Name);
        }

        [TestMethod]
        public async Task TestNameOfBlobInDirectory()
        {
            string blobName = "blob";

            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();
            CloudBlobDirectory directory = container.GetDirectoryReference("directory");
            CloudBlockBlob blob = directory.GetBlockBlobReference(blobName);
            await BlobUtils.CreateEmptyBlob(blob);
            AzureBlob azureBlob = new AzureBlob(blob);

            Assert.AreEqual(blobName, azureBlob.Name);
        }

        [TestMethod]
        public async Task TestUri()
        {
            CloudBlockBlob blob = await CreateEmptyBlob("blob");
            AzureBlob azureBlob = new AzureBlob(blob);

            Assert.AreEqual(blob.Uri.ToString(), azureBlob.Uri);
        }

        [TestMethod]
        public async Task TestDownloadText()
        {
            string text = "some text in the blob";
            CloudBlockBlob blob = await CreateEmptyBlob("blob");
            blob.UploadText(text);

            AzureBlob azureBlob = new AzureBlob(blob);
            Assert.AreEqual(text, azureBlob.DownloadText().Result);
        }

        [TestMethod]
        public async Task TestExists()
        {
            CloudBlockBlob blob = await CreateEmptyBlob("blob");
            AzureBlob azureBlob = new AzureBlob(blob);

            Assert.IsTrue(await azureBlob.Exists());

            blob.Delete();
            Assert.IsFalse(await azureBlob.Exists());
        }

        [TestMethod]
        public async Task TestDownload()
        {
            CloudBlockBlob blob = await CreateEmptyBlob("blob");
            AzureBlob azureBlob = new AzureBlob(blob);

            string path = Path.Combine(Path.GetTempPath(), "azureblobtest");
            File.Delete(path);

            await azureBlob.Download(path);
            Assert.IsTrue(File.Exists(path));
        }

        private async Task<CloudBlockBlob> CreateEmptyBlob(string blobName)
        {
            const string containerName = "container";
            CloudBlobContainer container = _blobClient.GetContainerReference(containerName);
            container.CreateIfNotExists();
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);
            await BlobUtils.CreateEmptyBlob(blob);
            return blob;
        }
    }
}
