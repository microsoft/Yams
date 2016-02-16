using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Azure.Utils;
using Etg.Yams.AzureTestUtils.Fixtures;
using Etg.Yams.Storage;
using Etg.Yams.Storage.Config;
using Etg.Yams.Utils;
using Microsoft.WindowsAzure.Storage.Blob;
using Xunit;

namespace Etg.Yams.Azure.Storage.Test
{
    public class BlobStorageDeploymentRepositoryTest : IClassFixture<AzureStorageEmulatorTestFixture>
    {
        private const string TestAppFileName = "AppConfig.json";
        private const string TestAppBlobRelPath = "app/1.0.0";
        private const string TestAppId = "app";
        private const string TestAppVersion = "1.0.0";
        private static readonly AppIdentity TestAppIdentity = new AppIdentity(TestAppId, new Version(TestAppVersion));

        private readonly string _deploymentConfigFilePath = Path.Combine("Data", "DeploymentRepository",
            "DeploymentConfig.json");

        private static CloudBlobClient _blobClient;
        private static IDeploymentRepository _deploymentRepository;


        public BlobStorageDeploymentRepositoryTest(AzureStorageEmulatorTestFixture fixture)
        {
            fixture.ClearBlobStorage();
            _blobClient = fixture.BlobClient;
            _deploymentRepository = new BlobStorageDeploymentRepository(Constants.EmulatorDataConnectionString);
        }

        [Fact]
        public async Task TestGetDeploymentConfigWhenTheFileIsNotThere()
        {
            DeploymentConfig deploymentConfig = await _deploymentRepository.FetchDeploymentConfig();
            Assert.False(deploymentConfig.ListApplications().Any());
        }

        [Fact]
        public async Task TestPublishThenFetchDeploymentConfig()
        {
            string data = File.ReadAllText(_deploymentConfigFilePath);
            DeploymentConfig deploymentConfig = new DeploymentConfig(data);
            await _deploymentRepository.PublishDeploymentConfig(deploymentConfig);
            DeploymentConfig newDeploymentConfig = await _deploymentRepository.FetchDeploymentConfig();
            Assert.Equal(deploymentConfig.RawData(), newDeploymentConfig.RawData());
        }

        [Fact]
        public async Task TestUploadApplicationBinaries()
        {
            const string someJsonContent = "some json content";
            await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, someJsonContent);
            await VerifyBlobStorageContent(someJsonContent);
        }

        [Fact]
        public async Task TestUploadApplicationBinaries_FailIfBinariesExistMode()
        {
            await Assert.ThrowsAsync<DuplicateBinariesException>(async () =>
            {
                const string originalJsonContent = "some json content";
                await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, originalJsonContent);
                await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, "different content");
                await VerifyBlobStorageContent(originalJsonContent);

            });
        }

        [Fact]
        public async Task TestUploadApplicationBinaries_DoNothingBinariesExistMode()
        {
            const string originalJsonContent = "some json content";
            await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, originalJsonContent);
            await UploadTestApplicationBinaries(ConflictResolutionMode.DoNothingIfBinariesExist, "different content");
            await VerifyBlobStorageContent(originalJsonContent);
        }

        [Fact]
        public async Task TestUploadApplicationBinaries_OverwriteExistingBinariesMode()
        {
            const string originalJsonContent = "some json content";
            await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, originalJsonContent);
            const string newContent = "different content";
            await UploadTestApplicationBinaries(ConflictResolutionMode.OverwriteExistingBinaries, newContent);
            await VerifyBlobStorageContent(newContent);
        }

        [Fact]
        public async Task TestUploadApplicationBinaries_EmptyBinariesDir()
        {
            const string testName = nameof(TestUploadApplicationBinaries_EmptyBinariesDir);
            await Assert.ThrowsAsync<BinariesNotFoundException>(async () =>
            {
                string localPath = await CreateTestTempDirectory(testName);
                await _deploymentRepository.UploadApplicationBinaries(TestAppIdentity, localPath, ConflictResolutionMode.OverwriteExistingBinaries);
            });
        }

        [Fact]
        public async Task TestUploadApplicationBinaries_NonExistingBinariesDir()
        {
            const string testName = nameof(TestUploadApplicationBinaries_NonExistingBinariesDir);
            await Assert.ThrowsAsync<BinariesNotFoundException>(async () =>
            {
                string localPath = Path.Combine(Path.GetTempPath(), testName);
                await FileUtils.DeleteDirectoryIfAny(localPath);
                await
                    _deploymentRepository.UploadApplicationBinaries(TestAppIdentity, localPath, ConflictResolutionMode.OverwriteExistingBinaries);
            });
        }

        [Fact]
        public async Task TestHasBinaries()
        {
            Assert.False(await _deploymentRepository.HasApplicationBinaries(TestAppIdentity));
            await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, "some json");
            Assert.True(await _deploymentRepository.HasApplicationBinaries(TestAppIdentity));
        }

        [Fact]
        public async Task TestDeleteApplicationBinaries()
        {
            await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, "some json");
            await _deploymentRepository.DeleteApplicationBinaries(TestAppIdentity);
            CloudBlobContainer applicationsContainer =
                _blobClient.GetContainerReference(BlobStorageDeploymentRepository.ApplicationsRootFolderName);
            Assert.False(await applicationsContainer.GetDirectoryReference(TestAppId).ExistsAsync());
        }

        [Fact]
        public async Task TestDeleteNonExistingApplicationBinaries()
        {
            await Assert.ThrowsAsync<BinariesNotFoundException>(async () =>
                await _deploymentRepository.DeleteApplicationBinaries(TestAppIdentity));
        }

        [Fact]
        public async Task TestDownloadApplicationBinaries()
        {
            const string testName = nameof(TestDownloadApplicationBinaries);
            const string testFileContent = "some content";
            await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, testFileContent);
            string localPath = await CreateTestTempDirectory(testName);
            await _deploymentRepository.DownloadApplicationBinaries(TestAppIdentity, localPath, ConflictResolutionMode.FailIfBinariesExist);
            VerifyBinariesExist(localPath, testFileContent);
        }

        [Fact]
        public async Task TestDownloadApplicationBinaries_ConflictResolutionMode()
        {
            // setup
            const string testName = nameof(ConflictResolutionMode);
            string localPath = await CreateTestTempDirectory(testName);
            CreateTestFile(localPath, "original content");
            VerifyBinariesExist(localPath, "original content");

            await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, "new content");
            await
                _deploymentRepository.DownloadApplicationBinaries(TestAppIdentity, localPath,
                    ConflictResolutionMode.DoNothingIfBinariesExist);
            VerifyBinariesExist(localPath, "original content");

            await
                _deploymentRepository.DownloadApplicationBinaries(TestAppIdentity, localPath,
                    ConflictResolutionMode.OverwriteExistingBinaries);
            VerifyBinariesExist(localPath, "new content");

            await Assert.ThrowsAsync<DuplicateBinariesException>(async () =>
                await
                _deploymentRepository.DownloadApplicationBinaries(TestAppIdentity, localPath, ConflictResolutionMode.FailIfBinariesExist));
        }

        [Fact]
        public async Task TestDownloadNonExistingApplicationBinaries()
        {
            const string testName = nameof(TestDownloadNonExistingApplicationBinaries);
            await Assert.ThrowsAsync<BinariesNotFoundException>(async () =>
            {
                string localPath = await CreateTestTempDirectory(testName);
                await
                    _deploymentRepository.DownloadApplicationBinaries(TestAppIdentity, localPath,
                        ConflictResolutionMode.OverwriteExistingBinaries);
            });
        }

        private static async Task VerifyBlobStorageContent(string someJsonContent)
        {
            CloudBlobContainer applicationsContainer =
                _blobClient.GetContainerReference(BlobStorageDeploymentRepository.ApplicationsRootFolderName);
            CloudBlobDirectory appDirectory = applicationsContainer.GetDirectoryReference(TestAppBlobRelPath);
            Assert.Equal(1, (await appDirectory.ListBlobsAsync()).Count());
            Assert.Equal(someJsonContent,
                await appDirectory.GetBlockBlobReference(TestAppFileName).DownloadTextAsync());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string GetCurrentMethodName()
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);
            return sf.GetMethod().Name;
        }

        private static void VerifyBinariesExist(string path, string testFileContent)
        {
            Assert.Equal(1, FileUtils.ListFilesRecursively(path).Count());
            Assert.Equal(testFileContent, File.ReadAllText(Path.Combine(path, TestAppFileName)));
        }

        private static async Task UploadTestApplicationBinaries(ConflictResolutionMode conflictResolutionMode, string testFileContent)
        {
            string testDir = await CreateTestTempDirectory(nameof(UploadTestApplicationBinaries));
            CreateTestFile(testDir, testFileContent);

            await _deploymentRepository.UploadApplicationBinaries(TestAppIdentity, testDir, conflictResolutionMode);
        }

        private static void CreateTestFile(string testDir, string testFileContent)
        {
            string appConfigPath = Path.Combine(testDir, TestAppFileName);
            File.WriteAllText(appConfigPath, testFileContent);
        }

        private static async Task<string> CreateTestTempDirectory(string testName)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), testName);
            await FileUtils.DeleteDirectoryIfAny(tempPath);
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }
    }
}