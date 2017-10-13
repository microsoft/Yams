using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Azure.Utils;
using Etg.Yams.AzureTestUtils.Fixtures;
using Etg.Yams.Utils;
using Microsoft.WindowsAzure.Storage.Blob;
using Xunit;
using System.Text;

namespace Etg.Yams.Azure.Test
{
    public class BlobUtilsTest : IClassFixture<AzureStorageEmulatorTestFixture>
    {
        private readonly CloudBlobClient _blobClient;

        public BlobUtilsTest(AzureStorageEmulatorTestFixture fixture)
        {
            fixture.ClearBlobStorage();
            _blobClient = fixture.BlobClient;
        }

        [Fact]
        public async Task TestDownloadBlobsFromBlobDirectory()
        {
            // First create the file hierarchy in the blob storage
            CloudBlobDirectory root = await CreateBlobsTree();

            // The upload the directory to a local temporary folder
            var tempPath = GetTestDirPath(nameof(TestDownloadBlobsFromBlobDirectory));
            var appPath = Path.Combine(tempPath, "app\\1.0.0");
            await BlobUtils.DownloadBlobDirectory(root, appPath);

            ISet<string> relativePathSet = new HashSet<string>();
            foreach (var path in FileUtils.ListFilesRecursively(appPath))
            {
                var relPath = FileUtils.GetRelativePath(appPath, path);
                relativePathSet.Add(relPath);
            }

            // Then verify that the hierachy on the local file system matches the one in the blob directory
            VerifyThatAllFilesAreThere(relativePathSet, "\\");
        }

        [Fact]
        public async Task TestDeleteBlobDirectory()
        {
            // First create the file hierarchy in the blob storage
            CloudBlobDirectory root = await CreateBlobsTree();
            await root.DeleteAsync();

            Assert.False(root.ListBlobs(true).Any());
        }

        [Fact]
        public async Task TestUploadFile()
        {
            var container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();

            var testDirPath = GetTestDirPath(nameof(TestUploadFile));
            var testFilePath = Path.Combine(testDirPath, "testFile.txt");
            var testFileContent = "TestUploadFileToBlob";
            File.WriteAllText(testFilePath, testFileContent);

            var testBlobPath = "testBlob.txt";
            await BlobUtils.UploadFile(testFilePath, container, testBlobPath);

            var blob = container.GetBlockBlobReference(testBlobPath);
            Assert.True(await blob.ExistsAsync());
            Assert.Equal(testFileContent, await blob.DownloadTextAsync());
        }

        [Fact]
        public async Task TestUploadDirectory()
        {
            // First create the file hierarchy on the local file system
            var rootPath = await CreateLocalFileTree(nameof(TestUploadDirectory));

            // Then upload the directory to blob storage
            var container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();
            string blobDirPath = $"{nameof(TestUploadDirectory)}/app/1.0.0";
            await BlobUtils.UploadDirectory(rootPath, container, blobDirPath);


            var blobDirectory = container.GetDirectoryReference(blobDirPath);
            var blobs = await blobDirectory.ListBlobsAsync();
            ISet<string> relativePathSet = new HashSet<string>();
            foreach (var blobItem in blobs)
            {
                var blob = (CloudBlockBlob) blobItem;
                var relPath = BlobUtils.GetBlobRelativePath(blob, blobDirectory);
                relativePathSet.Add(relPath);
            }

            // Then verify that the hierachy on the blob storage matches the one the local file system
            VerifyThatAllFilesAreThere(relativePathSet, "/");
        }

        [Fact]
        public async Task TestCreateBlobIfNotExists()
        {
            var container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();
            var blob = container.GetBlockBlobReference("createIfNotExistsTestBlob.txt");
            await BlobUtils.CreateBlobIfNotExists(blob);
            Assert.True(await blob.ExistsAsync());
            string text = "test content";
            await blob.UploadTextAsync(text);

            await BlobUtils.CreateBlobIfNotExists(blob);
            Assert.Equal(text, await blob.DownloadTextAsync());
        }

        private static void VerifyThatAllFilesAreThere(ISet<string> relativePathSet, string separator)
        {
            Assert.Equal(9, relativePathSet.Count);
            Assert.True(relativePathSet.Contains("b1"));
            Assert.True(relativePathSet.Contains("b2"));
            Assert.True(relativePathSet.Contains($"d1{separator}b1"));
            Assert.True(relativePathSet.Contains($"d1{separator}b2"));
            Assert.True(relativePathSet.Contains($"d2{separator}b3"));
            Assert.True(relativePathSet.Contains($"d2{separator}b4"));
            Assert.True(relativePathSet.Contains($"d2{separator}d3{separator}b5"));
            Assert.True(relativePathSet.Contains($"d2{separator}d3{separator}b6"));
            Assert.True(relativePathSet.Contains($"d4{separator}d5{separator}b7"));
        }

        private async Task<CloudBlobDirectory> CreateBlobsTree()
        {
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

            var container = _blobClient.GetContainerReference("container");
            container.CreateIfNotExists();

            var root = container.GetDirectoryReference("root");

            ICloudBlob b1 = root.GetBlockBlobReference("b1");
            await BlobUtils.CreateEmptyBlob(b1);

            ICloudBlob b2 = root.GetBlockBlobReference("b2");
            await BlobUtils.CreateEmptyBlob(b2);

            var d1 = root.GetDirectoryReference("d1");
            ICloudBlob d1b1 = d1.GetBlockBlobReference("b1");
            ICloudBlob d1b2 = d1.GetBlockBlobReference("b2");
            await BlobUtils.CreateEmptyBlob(d1b1);
            await BlobUtils.CreateEmptyBlob(d1b2);

            var d2 = root.GetDirectoryReference("d2");
            ICloudBlob d2b3 = d2.GetBlockBlobReference("b3");
            ICloudBlob d2b4 = d2.GetBlockBlobReference("b4");
            await BlobUtils.CreateEmptyBlob(d2b3);
            await BlobUtils.CreateEmptyBlob(d2b4);

            var d2d3 = d2.GetDirectoryReference("d3");
            ICloudBlob d2d3b5 = d2d3.GetBlockBlobReference("b5");
            ICloudBlob d2d3b6 = d2d3.GetBlockBlobReference("b6");
            await BlobUtils.CreateEmptyBlob(d2d3b5);
            await BlobUtils.CreateEmptyBlob(d2d3b6);

            await BlobUtils.CreateEmptyBlob(
                root.GetDirectoryReference("d4").GetDirectoryReference("d5").GetBlockBlobReference("b7"));
            return root;
        }

        private async Task<string> CreateLocalFileTree(string testName)
        {
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

            var testDirPath = GetTestDirPath(testName);
            var rootPath = Path.Combine(testDirPath, "root");

            await CreateNewDirectory(rootPath);
            CreateFile(rootPath, "b1");
            CreateFile(rootPath, "b2");

            var d1Path = Path.Combine(rootPath, "d1");
            Directory.CreateDirectory(d1Path);
            CreateFile(d1Path, "b1");
            CreateFile(d1Path, "b2");

            var d2Path = Path.Combine(rootPath, "d2");
            Directory.CreateDirectory(d2Path);
            CreateFile(d2Path, "b3");
            CreateFile(d2Path, "b4");

            var d3Path = Path.Combine(d2Path, "d3");
            Directory.CreateDirectory(d3Path);
            CreateFile(d3Path, "b5");
            CreateFile(d3Path, "b6");

            var d4Path = Path.Combine(rootPath, "d4");
            Directory.CreateDirectory(d4Path);
            var d5Path = Path.Combine(d4Path, "d5");
            Directory.CreateDirectory(d5Path);
            CreateFile(d5Path, "b7");
            return rootPath;
        }

        private async Task CreateNewDirectory(string path)
        {
            await FileUtils.DeleteDirectoryIfAny(path);
            Directory.CreateDirectory(path);
        }

        private void CreateFile(string dir, string relPath)
        {
            var path = Path.Combine(dir, relPath);
            // the file must contain something otherwise uploading to blob storage will fail
            File.WriteAllText(path, path);
        }

        private string GetTestDirPath(string testName)
        {
            var path = Path.Combine(Path.GetTempPath(), nameof(BlobUtilsTest), testName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
    }
}