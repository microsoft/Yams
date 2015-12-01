using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AzureTestUtils;
using Etg.Yams.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Test.Utils
{
    [TestClass]
    public class BlobUtilsTest
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
        public async Task TestDownloadBlobsFromBlobDirectory()
        {
            CloudBlobContainer container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();

            CloudBlobDirectory root = container.GetDirectoryReference("root");

            ICloudBlob b1 = root.GetBlockBlobReference("b1");
            await BlobUtils.CreateEmptyBlob(b1);

            ICloudBlob b2 = root.GetBlockBlobReference("b2");
            await BlobUtils.CreateEmptyBlob(b2);

            CloudBlobDirectory d1 = root.GetDirectoryReference("d1");
            ICloudBlob d1b1 = d1.GetBlockBlobReference("b1");
            ICloudBlob d1b2 = d1.GetBlockBlobReference("b2");
            await BlobUtils.CreateEmptyBlob(d1b1);
            await BlobUtils.CreateEmptyBlob(d1b2);

            CloudBlobDirectory d2 = root.GetDirectoryReference("d2");
            ICloudBlob d2b3 = d2.GetBlockBlobReference("b3");
            ICloudBlob d2b4 = d2.GetBlockBlobReference("b4");
            await BlobUtils.CreateEmptyBlob(d2b3);
            await BlobUtils.CreateEmptyBlob(d2b4);

            CloudBlobDirectory d2d3 = d2.GetDirectoryReference("d3");
            ICloudBlob d2d3b5 = d2d3.GetBlockBlobReference("b5");
            ICloudBlob d2d3b6 = d2d3.GetBlockBlobReference("b6");
            await BlobUtils.CreateEmptyBlob(d2d3b5);
            await BlobUtils.CreateEmptyBlob(d2d3b6);

            await BlobUtils.CreateEmptyBlob(
                root.GetDirectoryReference("d4").GetDirectoryReference("d5").GetBlockBlobReference("b7"));

            // The hierarchy in the blob storage is as follows:
            //
            // root
            // |__b1
            // |__b2
            // |__d1
            // |  |__b1
            // |  |__b2
            // |__d2
            // |  |__b3
            // |  |__b4
            // |  |__d3
            // |  |  |__b5
            // |  |  |__b6
            // |__d4
            // |  |__d5
            // |  |  |__b7

            string tempPath = Path.Combine(Path.GetTempPath(), "TestDownloadBlobsFromBlobDirectory");
            await BlobUtils.DownloadBlobDirectory(root, tempPath);

            ISet<string> relativePathSet = new HashSet<string>();
            foreach (string path in Directory.GetFiles(tempPath, "*.*", SearchOption.AllDirectories))
            {
                string hash = path.Remove(0, tempPath.Length);
                relativePathSet.Add(hash);
            }

            Assert.AreEqual(9, relativePathSet.Count);
            Assert.IsTrue(relativePathSet.Contains("\\b1"));
            Assert.IsTrue(relativePathSet.Contains("\\b2"));
            Assert.IsTrue(relativePathSet.Contains("\\d1\\b1"));
            Assert.IsTrue(relativePathSet.Contains("\\d1\\b2"));
            Assert.IsTrue(relativePathSet.Contains("\\d2\\b3"));
            Assert.IsTrue(relativePathSet.Contains("\\d2\\b4"));
            Assert.IsTrue(relativePathSet.Contains("\\d2\\d3\\b5"));
            Assert.IsTrue(relativePathSet.Contains("\\d2\\d3\\b6"));
            Assert.IsTrue(relativePathSet.Contains("\\d4\\d5\\b7"));        
        }
    }
}
