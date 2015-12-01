using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class AzureBlobContainerTest
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
        public void TestName()
        {
            const string containerName = "container";
            CloudBlobContainer container = _blobClient.GetContainerReference(containerName);
            container.CreateIfNotExists();

            AzureBlobContainer azureBlobContainer = new AzureBlobContainer(container);
            Assert.AreEqual(containerName, azureBlobContainer.Name);
        }

        [TestMethod]
        public void TestUri()
        {
            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();

            AzureBlobContainer azureBlobContainer = new AzureBlobContainer(container);
            Assert.AreEqual(container.Uri.ToString(), azureBlobContainer.Uri);
        }

        [TestMethod]
        public async Task TestDirectories()
        {
            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();
            
            await Task.WhenAll(
                CreateNonEmptyDirectory(container, "dir1"),
                CreateNonEmptyDirectory(container, "dir2"));

            AzureBlobContainer azureBlobContainer = new AzureBlobContainer(container);
            IEnumerable<string> directories = (await azureBlobContainer.ListDirectories()).Select(d => d.Name);

            Assert.AreEqual(2, directories.Count());
            Assert.IsTrue(directories.Contains("dir1"));
            Assert.IsTrue(directories.Contains("dir2"));
        }

        [TestMethod]
        public async Task TestFiles()
        {
            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();

            await Task.WhenAll(
                CreateEmptyBlob(container.GetBlockBlobReference("blob1")),
                CreateEmptyBlob(container.GetBlockBlobReference("blob2")));

            AzureBlobContainer azureBlobContainer = new AzureBlobContainer(container);
            IEnumerable<string> files = (await azureBlobContainer.ListFiles()).Select(d => d.Name);

            Assert.AreEqual(2, files.Count());
            Assert.IsTrue(files.Contains("blob1"));
            Assert.IsTrue(files.Contains("blob2"));
        }

        [TestMethod]
        public async Task TestGetDirectory()
        {
            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();

            await CreateNonEmptyDirectory(container, "dir1");
            AzureBlobContainer azureBlobContainer = new AzureBlobContainer(container);
            
            Assert.AreEqual("dir1", (await azureBlobContainer.GetDirectory("dir1")).Name);
        }

        [TestMethod]
        public async Task TestGetFile()
        {
            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();

            await CreateEmptyBlob(container.GetBlockBlobReference("blob1"));
            AzureBlobContainer azureBlobContainer = new AzureBlobContainer(container);

            Assert.AreEqual("blob1", (await azureBlobContainer.GetFile("blob1")).Name);
        }

        [TestMethod]
        public async Task TestExists()
        {
            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();
            await CreateEmptyBlob(container.GetBlockBlobReference("blob1"));

            AzureBlobContainer azureBlobContainer = new AzureBlobContainer(container);
            Assert.IsTrue(await azureBlobContainer.Exists());

            container.Delete();
            Assert.IsFalse(await azureBlobContainer.Exists());
        }

        [TestMethod]
        public async Task TestDownload()
        {
            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();
            await CreateEmptyBlob(container.GetBlockBlobReference("blob"));

            AzureBlobContainer azureBlobContainer = new AzureBlobContainer(container);

            string path = Path.Combine(Path.GetTempPath(), "azureblobcontainertest");
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }

            await azureBlobContainer.Download(path);
            Assert.IsTrue(Directory.Exists(path));
        }

        private async Task CreateNonEmptyDirectory(CloudBlobContainer container, string directoryName)
        {
            var dir = container.GetDirectoryReference(directoryName);
            var blob = dir.GetBlockBlobReference("blob");
            await CreateEmptyBlob(blob);
        }

        private async Task CreateEmptyBlob(ICloudBlob blob)
        {
            await BlobUtils.CreateEmptyBlob(blob);
        }
    }
}
