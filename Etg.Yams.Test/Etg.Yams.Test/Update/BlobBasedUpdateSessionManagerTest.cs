using System.Threading.Tasks;
using AzureTestUtils;
using Etg.Yams.Lease;
using Etg.Yams.Update;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Test.Update
{
    [TestClass]
    public class BlobBasedUpdateSessionManagerTest
    {
        private static CloudStorageAccount _account;
        private static CloudBlobClient _blobClient;
        private static StorageEmulatorProxy _storageEmulatorProxy;
        private static string _storageContainerName;
        private static BlobLeaseFactory _blobLeaseFactory;

        [ClassInitialize]
        public static void StartAndCleanStorage(TestContext cont)
        {
            _account = CloudStorageAccount.DevelopmentStorageAccount;
            _blobClient = _account.CreateCloudBlobClient();

            _storageEmulatorProxy = new StorageEmulatorProxy();
            _storageEmulatorProxy.StartEmulator();

            _storageContainerName = Constants.ApplicationsRootFolderName;

            _blobLeaseFactory = new BlobLeaseFactory(60);
        }

        [ClassCleanup]
        public static void ShutdownStorage()
        {
            _storageEmulatorProxy.StopEmulator();
        }

        [TestInitialize]
        public void InitializeStorage()
        {
            _storageEmulatorProxy.ClearBlobStorage();
            CloudBlobContainer container = _blobClient.GetContainerReference(_storageContainerName);
            container.CreateIfNotExists();
        }

        [TestMethod]
        public async Task TestStartUpdateSessionSimple()
        {
            UpdateSessionManagerConfig config = new UpdateSessionManagerConfig("cloudServiceDeploymentId", "1", "instanceId", _storageContainerName);
            BlobBasedUpdateSessionManager updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.IsTrue(await updateSessionManager.TryStartUpdateSession("app1"));
        }

        [TestMethod]
        public async Task TestThatOnlyOneUpdateDomainCanUpdateAtATime()
        {
            UpdateSessionManagerConfig config = new UpdateSessionManagerConfig("cloudServiceDeploymentId", "1", "instanceId1", _storageContainerName);
            BlobBasedUpdateSessionManager updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.IsTrue(await updateSessionManager.TryStartUpdateSession("app1"));

            config = new UpdateSessionManagerConfig("cloudServiceDeploymentId", "2", "instanceId2", _storageContainerName);
            updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.IsFalse(await updateSessionManager.TryStartUpdateSession("app1"));
        }

        [TestMethod]
        public async Task TestThatMultipleInstancesInTheSameUpdateDomainCanUpdateSimultaneously()
        {
            UpdateSessionManagerConfig config = new UpdateSessionManagerConfig("cloudServiceDeploymentId", "1", "instanceId1", _storageContainerName);
            BlobBasedUpdateSessionManager updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.IsTrue(await updateSessionManager.TryStartUpdateSession("app1"));

            config = new UpdateSessionManagerConfig("cloudServiceDeploymentId", "1", "instanceId2", _storageContainerName);
            updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.IsTrue(await updateSessionManager.TryStartUpdateSession("app1"));
        }

        [TestMethod]
        public async Task TestThatEndUpdateSessionWorks()
        {
            UpdateSessionManagerConfig config = new UpdateSessionManagerConfig("cloudServiceDeploymentId", "1", "instanceId1", _storageContainerName);
            BlobBasedUpdateSessionManager updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.IsTrue(await updateSessionManager.TryStartUpdateSession("app1"));

            await updateSessionManager.EndUpdateSession("app1");

            config = new UpdateSessionManagerConfig("cloudServiceDeploymentId", "2", "instanceId2", _storageContainerName);
            updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.IsTrue(await updateSessionManager.TryStartUpdateSession("app1"));
        }

        [TestMethod]
        public async Task TestThatDifferentClustersCanUpdateIndependently()
        {
            UpdateSessionManagerConfig config = new UpdateSessionManagerConfig("cloudServiceDeploymentId", "1", "instanceId1", _storageContainerName);
            BlobBasedUpdateSessionManager updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.IsTrue(await updateSessionManager.TryStartUpdateSession("app1"));

            config = new UpdateSessionManagerConfig("cloudServiceDeploymentId2", "2", "instanceId2", _storageContainerName);
            updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.IsTrue(await updateSessionManager.TryStartUpdateSession("app1"));
        }
    }
}
