using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AzureTestUtils;
using Etg.Yams.Application;
using Etg.Yams.Storage;
using Etg.Yams.Storage.Config;
using Etg.Yams.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Test.Storage
{
    [TestClass]
    public class YamsRepositoryTest
    {
        private const string TestAppFileName = "AppConfig.json";
        private const string TestAppBlobRelPath = "app/1.0.0";
        private const string TestAppId = "app";
        private const string TestAppVersion = "1.0.0";
        private static readonly AppIdentity TestAppIdentity = new AppIdentity(TestAppId, new Version(TestAppVersion));

        private readonly string _deploymentConfigFilePath = Path.Combine("Data", "YamsRepository",
            "DeploymentConfig.json");

        private static CloudStorageAccount _account;
        private static CloudBlobClient _blobClient;
        private static StorageEmulatorProxy _storageEmulatorProxy;
        private static IDeploymentRepository _deploymentRepository;

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
        public void TestInitialize()
        {
            _storageEmulatorProxy.ClearBlobStorage();
            _deploymentRepository = new BlobStorageDeploymentRepository(Constants.EmulatorDataConnectionString);
        }

        [TestMethod]
        public async Task TestGetDeploymentConfigWhenTheFileIsNotThere()
        {
            DeploymentConfig deploymentConfig = await _deploymentRepository.FetchDeploymentConfig();
            Assert.IsFalse(deploymentConfig.ListApplications().Any());
        }

        [TestMethod]
        public async Task TestPublishThenFetchDeploymentConfig()
        {
            string data = File.ReadAllText(_deploymentConfigFilePath);
            DeploymentConfig deploymentConfig = new DeploymentConfig(data);
            await _deploymentRepository.PublishDeploymentConfig(deploymentConfig);
            DeploymentConfig newDeploymentConfig = await _deploymentRepository.FetchDeploymentConfig();
            Assert.AreEqual(deploymentConfig.RawData(), newDeploymentConfig.RawData());
        }

        [TestMethod]
        public async Task TestUploadApplicationBinaries()
        {
            const string someJsonContent = "some json content";
            await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, someJsonContent);
            await VerifyBlobStorageContent(someJsonContent);
        }

        [TestMethod, ExpectedException(typeof (DuplicateBinariesException))]
        public async Task TestUploadApplicationBinaries_FailIfBinariesExistMode()
        {
            const string originalJsonContent = "some json content";
            await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, originalJsonContent);
            await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, "different content");
            await VerifyBlobStorageContent(originalJsonContent);
        }

        [TestMethod]
        public async Task TestUploadApplicationBinaries_DoNothingBinariesExistMode()
        {
            const string originalJsonContent = "some json content";
            await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, originalJsonContent);
            await UploadTestApplicationBinaries(ConflictResolutionMode.DoNothingIfBinariesExist, "different content");
            await VerifyBlobStorageContent(originalJsonContent);
        }

        [TestMethod]
        public async Task TestUploadApplicationBinaries_OverwriteExistingBinariesMode()
        {
            const string originalJsonContent = "some json content";
            await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, originalJsonContent);
            const string newContent = "different content";
            await UploadTestApplicationBinaries(ConflictResolutionMode.OverwriteExistingBinaries, newContent);
            await VerifyBlobStorageContent(newContent);
        }

        [TestMethod, ExpectedException(typeof (BinariesNotFoundException))]
        public async Task TestUploadApplicationBinaries_EmptyBinariesDir()
        {
            const string testName = nameof(TestUploadApplicationBinaries_EmptyBinariesDir);
            string localPath = await CreateTestTempDirectory(testName);
            await
                _deploymentRepository.UploadApplicationBinaries(TestAppIdentity, localPath, ConflictResolutionMode.OverwriteExistingBinaries);
        }

        [TestMethod, ExpectedException(typeof (BinariesNotFoundException))]
        public async Task TestUploadApplicationBinaries_NonExistingBinariesDir()
        {
            const string testName = nameof(TestUploadApplicationBinaries_NonExistingBinariesDir);
            string localPath = Path.Combine(Path.GetTempPath(), testName);
            await FileUtils.DeleteDirectoryIfAny(localPath);
            await
                _deploymentRepository.UploadApplicationBinaries(TestAppIdentity, localPath, ConflictResolutionMode.OverwriteExistingBinaries);
        }

        [TestMethod]
        public async Task TestHasBinaries()
        {
            Assert.IsFalse(await _deploymentRepository.HasApplicationBinaries(TestAppIdentity));
            await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, "some json");
            Assert.IsTrue(await _deploymentRepository.HasApplicationBinaries(TestAppIdentity));
        }

        [TestMethod]
        public async Task TestDeleteApplicationBinaries()
        {
            await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, "some json");
            await _deploymentRepository.DeleteApplicationBinaries(TestAppIdentity);
            CloudBlobContainer applicationsContainer =
                _blobClient.GetContainerReference(Constants.ApplicationsRootFolderName);
            Assert.IsFalse(await applicationsContainer.GetDirectoryReference(TestAppId).ExistsAsync());
        }

        [TestMethod, ExpectedException(typeof (BinariesNotFoundException))]
        public async Task TestDeleteNonExistingApplicationBinaries()
        {
            await _deploymentRepository.DeleteApplicationBinaries(TestAppIdentity);
        }

        [TestMethod]
        public async Task TestDownloadApplicationBinaries()
        {
            const string testName = nameof(TestDownloadApplicationBinaries);
            const string testFileContent = "some content";
            await UploadTestApplicationBinaries(ConflictResolutionMode.FailIfBinariesExist, testFileContent);
            string localPath = await CreateTestTempDirectory(testName);
            await _deploymentRepository.DownloadApplicationBinaries(TestAppIdentity, localPath, ConflictResolutionMode.FailIfBinariesExist);
            VerifyBinariesExist(localPath, testFileContent);
        }

        [TestMethod]
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

            try
            {
                await
                    _deploymentRepository.DownloadApplicationBinaries(TestAppIdentity, localPath, ConflictResolutionMode.FailIfBinariesExist);
                Assert.Fail($"A {nameof(DuplicateBinariesException)} was expected");
            }
            catch (DuplicateBinariesException)
            {
            }
        }

        [TestMethod, ExpectedException(typeof (BinariesNotFoundException))]
        public async Task TestDownloadNonExistingApplicationBinaries()
        {
            const string testName = nameof(TestDownloadNonExistingApplicationBinaries);
            string localPath = await CreateTestTempDirectory(testName);
            await
                _deploymentRepository.DownloadApplicationBinaries(TestAppIdentity, localPath,
                    ConflictResolutionMode.OverwriteExistingBinaries);
        }

        private static async Task VerifyBlobStorageContent(string someJsonContent)
        {
            CloudBlobContainer applicationsContainer =
                _blobClient.GetContainerReference(Constants.ApplicationsRootFolderName);
            CloudBlobDirectory appDirectory = applicationsContainer.GetDirectoryReference(TestAppBlobRelPath);
            Assert.AreEqual(1, (await appDirectory.ListBlobsAsync()).Count());
            Assert.AreEqual(someJsonContent,
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
            Assert.AreEqual(1, FileUtils.ListFilesRecursively(path).Count());
            Assert.AreEqual(testFileContent, File.ReadAllText(Path.Combine(path, TestAppFileName)));
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