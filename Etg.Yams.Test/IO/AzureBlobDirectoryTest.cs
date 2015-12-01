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
    public class AzureBlobDirectoryTest
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
            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();

            const string dirName = "dir";
            await CreateNonEmptyDirectory(container, dirName);

            AzureBlobDirectory azureBlobDirectory = new AzureBlobDirectory(container.GetDirectoryReference(dirName));
            Assert.AreEqual(dirName, azureBlobDirectory.Name);
        }

        [TestMethod]
        public async Task TestUri()
        {
            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();

            const string dirName = "dir";
            CloudBlobDirectory directory = await CreateNonEmptyDirectory(container, dirName);

            AzureBlobDirectory azureBlobDirectory = new AzureBlobDirectory(container.GetDirectoryReference(dirName));
            Assert.AreEqual(directory.Uri.ToString(), azureBlobDirectory.Uri);
        }

        [TestMethod]
        public async Task TestDirectories()
        {
            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();

            CloudBlobDirectory directory = await CreateNonEmptyDirectory(container, "dir");

            await Task.WhenAll(
                CreateNonEmptyDirectory(directory, "dir1"),
                CreateNonEmptyDirectory(directory, "dir2"));

            AzureBlobDirectory azureBlobDirectory = new AzureBlobDirectory(directory);
            IEnumerable<string> directories = (await azureBlobDirectory.ListDirectories()).Select(d => d.Name);

            Assert.AreEqual(2, directories.Count());
            Assert.IsTrue(directories.Contains("dir1"));
            Assert.IsTrue(directories.Contains("dir2"));
        }

        [TestMethod]
        public async Task TestFiles()
        {
            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();

            CloudBlobDirectory directory = container.GetDirectoryReference("dir");

            await Task.WhenAll(
                CreateEmptyBlob(directory.GetBlockBlobReference("blob1")),
                CreateEmptyBlob(directory.GetBlockBlobReference("blob2")));

            AzureBlobDirectory azureBlobDirectory = new AzureBlobDirectory(directory);
            IEnumerable<string> files = (await azureBlobDirectory.ListFiles()).Select(d => d.Name);

            Assert.AreEqual(2, files.Count());
            Assert.IsTrue(files.Contains("blob1"));
            Assert.IsTrue(files.Contains("blob2"));
        }

        [TestMethod]
        public async Task TestGetDirectory()
        {
            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();

            CloudBlobDirectory directory = await CreateNonEmptyDirectory(container, "dir");

            await CreateNonEmptyDirectory(directory, "dir1");
            AzureBlobDirectory azureBlobDirectory = new AzureBlobDirectory(directory);

            Assert.AreEqual("dir1", (await azureBlobDirectory.GetDirectory("dir1")).Name);
        }

        [TestMethod]
        public async Task TestGetFile()
        {
            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();

            CloudBlobDirectory directory = await CreateNonEmptyDirectory(container, "dir");

            await CreateEmptyBlob(directory.GetBlockBlobReference("blob1"));
            AzureBlobDirectory azureBlobDirectory = new AzureBlobDirectory(directory);

            Assert.AreEqual("blob1", (await azureBlobDirectory.GetFile("blob1")).Name);
        }

        [TestMethod]
        public async Task TestExists()
        {
            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();
            await CreateEmptyBlob(container.GetBlockBlobReference("blob1"));

            CloudBlobDirectory directory = await CreateNonEmptyDirectory(container, "dir");

            AzureBlobDirectory azureBlobDirectory = new AzureBlobDirectory(directory);
            Assert.IsTrue(await azureBlobDirectory.Exists());

            await DeleteDirectory(directory);
            Assert.IsFalse(await azureBlobDirectory.Exists());
        }

        [TestMethod]
        public async Task TestDownload()
        {
            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();

            CloudBlobDirectory directory = await CreateNonEmptyDirectory(container, "dir");
            AzureBlobDirectory azureBlobDirectory = new AzureBlobDirectory(directory);

            string path = Path.Combine(Path.GetTempPath(), "azureblobdirectorydownloadtest");
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }

            await azureBlobDirectory.Download(path);
            Assert.IsTrue(Directory.Exists(path));
        }

        private static Task DeleteDirectory(CloudBlobDirectory directory)
        {
            return directory.GetBlockBlobReference("blob").DeleteAsync();
        }

        private Task<CloudBlobDirectory> CreateNonEmptyDirectory(CloudBlobContainer container, string directoryName)
        {
            return CreateNonEmptyDir((dynamic)container, directoryName);
        }

        private Task<CloudBlobDirectory> CreateNonEmptyDirectory(CloudBlobDirectory parent, string directoryName)
        {
            return CreateNonEmptyDir((dynamic)parent, directoryName);
        }

        private async Task<CloudBlobDirectory> CreateNonEmptyDir(dynamic parent, string directoryName)
        {
            var dir = parent.GetDirectoryReference(directoryName);
            var blob = dir.GetBlockBlobReference("blob");
            await CreateEmptyBlob(blob);
            return dir;
        }

        private Task CreateEmptyBlob(ICloudBlob blob)
        {
            return BlobUtils.CreateEmptyBlob(blob);
        }
    }
}
