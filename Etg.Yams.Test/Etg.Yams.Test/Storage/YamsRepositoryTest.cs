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
using FileMode = Etg.Yams.Storage.FileMode;

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
        private static IYamsRepository _yamsRepository;

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

            IYamsRepositoryFactory yamsRepositoryFactory = new YamsRepositoryFactory();
            _yamsRepository = yamsRepositoryFactory.CreateRepository(Constants.EmulatorDataConnectionString);
        }

        [TestMethod]
        public async Task TestGetDeploymentConfigWhenTheFileIsNotThere()
        {
            DeploymentConfig deploymentConfig = await _yamsRepository.FetchDeploymentConfig();
            Assert.IsFalse(deploymentConfig.ListApplications().Any());
        }

        [TestMethod]
        public async Task TestPublishThenFetchDeploymentConfig()
        {
            string data = File.ReadAllText(_deploymentConfigFilePath);
            DeploymentConfig deploymentConfig = new DeploymentConfig(data);
            await _yamsRepository.PublishDeploymentConfig(deploymentConfig);
            DeploymentConfig newDeploymentConfig = await _yamsRepository.FetchDeploymentConfig();
            Assert.AreEqual(deploymentConfig.RawData(), newDeploymentConfig.RawData());
        }

        [TestMethod]
        public async Task TestUploadApplicationBinaries()
        {
            const string someJsonContent = "some json content";
            await UploadTestApplicationBinaries(FileMode.FailIfBinariesExist, someJsonContent);
            await VerifyBlobStorageContent(someJsonContent);
        }

        [TestMethod, ExpectedException(typeof (DuplicateBinariesException))]
        public async Task TestUploadApplicationBinaries_FailIfBinariesExistMode()
        {
            const string originalJsonContent = "some json content";
            await UploadTestApplicationBinaries(FileMode.FailIfBinariesExist, originalJsonContent);
            await UploadTestApplicationBinaries(FileMode.FailIfBinariesExist, "different content");
            await VerifyBlobStorageContent(originalJsonContent);
        }

        [TestMethod]
        public async Task TestUploadApplicationBinaries_DoNothingBinariesExistMode()
        {
            const string originalJsonContent = "some json content";
            await UploadTestApplicationBinaries(FileMode.FailIfBinariesExist, originalJsonContent);
            await UploadTestApplicationBinaries(FileMode.DoNothingIfBinariesExist, "different content");
            await VerifyBlobStorageContent(originalJsonContent);
        }

        [TestMethod]
        public async Task TestUploadApplicationBinaries_OverwriteExistingBinariesMode()
        {
            const string originalJsonContent = "some json content";
            await UploadTestApplicationBinaries(FileMode.FailIfBinariesExist, originalJsonContent);
            const string newContent = "different content";
            await UploadTestApplicationBinaries(FileMode.OverwriteExistingBinaries, newContent);
            await VerifyBlobStorageContent(newContent);
        }

        [TestMethod, ExpectedException(typeof (BinariesNotFoundException))]
        public async Task TestUploadApplicationBinaries_EmptyBinariesDir()
        {
            const string testName = nameof(TestUploadApplicationBinaries_EmptyBinariesDir);
            string localPath = await CreateTestTempDirectory(testName);
            await
                _yamsRepository.UploadApplicationBinaries(TestAppIdentity, localPath, FileMode.OverwriteExistingBinaries);
        }

        [TestMethod, ExpectedException(typeof (BinariesNotFoundException))]
        public async Task TestUploadApplicationBinaries_NonExistingBinariesDir()
        {
            const string testName = nameof(TestUploadApplicationBinaries_NonExistingBinariesDir);
            string localPath = Path.Combine(Path.GetTempPath(), testName);
            await FileUtils.DeleteDirectoryIfAny(localPath);
            await
                _yamsRepository.UploadApplicationBinaries(TestAppIdentity, localPath, FileMode.OverwriteExistingBinaries);
        }

        [TestMethod]
        public async Task TestHasBinaries()
        {
            Assert.IsFalse(await _yamsRepository.HasApplicationBinaries(TestAppIdentity));
            await UploadTestApplicationBinaries(FileMode.FailIfBinariesExist, "some json");
            Assert.IsTrue(await _yamsRepository.HasApplicationBinaries(TestAppIdentity));
        }

        [TestMethod]
        public async Task TestDeleteApplicationBinaries()
        {
            await UploadTestApplicationBinaries(FileMode.FailIfBinariesExist, "some json");
            await _yamsRepository.DeleteApplicationBinaries(TestAppIdentity);
            CloudBlobContainer applicationsContainer =
                _blobClient.GetContainerReference(Constants.ApplicationsRootFolderName);
            Assert.IsFalse(await applicationsContainer.GetDirectoryReference(TestAppId).ExistsAsync());
        }

        [TestMethod, ExpectedException(typeof (BinariesNotFoundException))]
        public async Task TestDeleteNonExistingApplicationBinaries()
        {
            await _yamsRepository.DeleteApplicationBinaries(TestAppIdentity);
        }

        [TestMethod]
        public async Task TestDownloadApplicationBinaries()
        {
            const string testName = nameof(TestDownloadApplicationBinaries);
            const string testFileContent = "some content";
            await UploadTestApplicationBinaries(FileMode.FailIfBinariesExist, testFileContent);
            string localPath = await CreateTestTempDirectory(testName);
            await _yamsRepository.DownloadApplicationBinaries(TestAppIdentity, localPath, FileMode.FailIfBinariesExist);
            VerifyBinariesExist(localPath, testFileContent);
        }

        [TestMethod]
        public async Task TestDownloadApplicationBinaries_FileMode()
        {
            // setup
            const string testName = nameof(TestDownloadApplicationBinaries_FileMode);
            string localPath = await CreateTestTempDirectory(testName);
            CreateTestFile(localPath, "original content");
            VerifyBinariesExist(localPath, "original content");

            await UploadTestApplicationBinaries(FileMode.FailIfBinariesExist, "new content");
            await
                _yamsRepository.DownloadApplicationBinaries(TestAppIdentity, localPath,
                    FileMode.DoNothingIfBinariesExist);
            VerifyBinariesExist(localPath, "original content");

            await
                _yamsRepository.DownloadApplicationBinaries(TestAppIdentity, localPath,
                    FileMode.OverwriteExistingBinaries);
            VerifyBinariesExist(localPath, "new content");

            try
            {
                await
                    _yamsRepository.DownloadApplicationBinaries(TestAppIdentity, localPath, FileMode.FailIfBinariesExist);
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
                _yamsRepository.DownloadApplicationBinaries(TestAppIdentity, localPath,
                    FileMode.OverwriteExistingBinaries);
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

        private static async Task UploadTestApplicationBinaries(FileMode fileMode, string testFileContent)
        {
            string testDir = await CreateTestTempDirectory(nameof(UploadTestApplicationBinaries));
            CreateTestFile(testDir, testFileContent);

            await _yamsRepository.UploadApplicationBinaries(TestAppIdentity, testDir, fileMode);
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