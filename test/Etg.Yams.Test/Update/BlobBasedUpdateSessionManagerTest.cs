using System.Threading.Tasks;
using AzureTestUtils;
using Etg.Yams.Lease;
using Etg.Yams.Update;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using Xunit;

namespace Etg.Yams.Test.Update
{
    public class BlobBasedUpdateSessionManagerTestFixture : IDisposable
    {
        private static CloudStorageAccount _account;
        public CloudBlobClient BlobClient { get; private set; }
        private static StorageEmulatorProxy _storageEmulatorProxy;
        public string StorageContainerName { get; private set; }

        public BlobLeaseFactory BlobLeaseFactory { get; private set; }

        public BlobBasedUpdateSessionManagerTestFixture()
        {
            _account = CloudStorageAccount.DevelopmentStorageAccount;
            BlobClient = _account.CreateCloudBlobClient();

            _storageEmulatorProxy = new StorageEmulatorProxy();
            _storageEmulatorProxy.StartEmulator();

            StorageContainerName = Constants.ApplicationsRootFolderName;

            BlobLeaseFactory = new BlobLeaseFactory(60);
        }

        public void Dispose()
        {
            _storageEmulatorProxy.StopEmulator();
        }
    }

    public class BlobBasedUpdateSessionManagerTest : IClassFixture<BlobBasedUpdateSessionManagerTestFixture>
    {
        private CloudBlobClient _blobClient;
        private string _storageContainerName;
        private BlobLeaseFactory _blobLeaseFactory;

        public BlobBasedUpdateSessionManagerTest(BlobBasedUpdateSessionManagerTestFixture fixture)
        {
            _blobClient = fixture.BlobClient;
            _blobLeaseFactory = fixture.BlobLeaseFactory;
            _storageContainerName = fixture.StorageContainerName;
        }

        [Fact]
        public async Task TestStartUpdateSessionSimple()
        {
            UpdateSessionManagerConfig config = new UpdateSessionManagerConfig("cloudServiceDeploymentId", "1", "instanceId", _storageContainerName);
            BlobBasedUpdateSessionManager updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.True(await updateSessionManager.TryStartUpdateSession("app1"));
        }

        [Fact]
        public async Task TestThatOnlyOneUpdateDomainCanUpdateAtATime()
        {
            UpdateSessionManagerConfig config = new UpdateSessionManagerConfig("cloudServiceDeploymentId", "1", "instanceId1", _storageContainerName);
            BlobBasedUpdateSessionManager updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.True(await updateSessionManager.TryStartUpdateSession("app1"));

            config = new UpdateSessionManagerConfig("cloudServiceDeploymentId", "2", "instanceId2", _storageContainerName);
            updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.False(await updateSessionManager.TryStartUpdateSession("app1"));
        }

        [Fact]
        public async Task TestThatMultipleInstancesInTheSameUpdateDomainCanUpdateSimultaneously()
        {
            UpdateSessionManagerConfig config = new UpdateSessionManagerConfig("cloudServiceDeploymentId", "1", "instanceId1", _storageContainerName);
            BlobBasedUpdateSessionManager updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.True(await updateSessionManager.TryStartUpdateSession("app1"));

            config = new UpdateSessionManagerConfig("cloudServiceDeploymentId", "1", "instanceId2", _storageContainerName);
            updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.True(await updateSessionManager.TryStartUpdateSession("app1"));
        }

        [Fact]
        public async Task TestThatEndUpdateSessionWorks()
        {
            UpdateSessionManagerConfig config = new UpdateSessionManagerConfig("cloudServiceDeploymentId", "1", "instanceId1", _storageContainerName);
            BlobBasedUpdateSessionManager updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.True(await updateSessionManager.TryStartUpdateSession("app1"));

            await updateSessionManager.EndUpdateSession("app1");

            config = new UpdateSessionManagerConfig("cloudServiceDeploymentId", "2", "instanceId2", _storageContainerName);
            updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.True(await updateSessionManager.TryStartUpdateSession("app1"));
        }

        [Fact]
        public async Task TestThatDifferentClustersCanUpdateIndependently()
        {
            UpdateSessionManagerConfig config = new UpdateSessionManagerConfig("cloudServiceDeploymentId", "1", "instanceId1", _storageContainerName);
            BlobBasedUpdateSessionManager updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.True(await updateSessionManager.TryStartUpdateSession("app1"));

            config = new UpdateSessionManagerConfig("cloudServiceDeploymentId2", "2", "instanceId2", _storageContainerName);
            updateSessionManager = new BlobBasedUpdateSessionManager(config, _blobClient, _blobLeaseFactory);
            Assert.True(await updateSessionManager.TryStartUpdateSession("app1"));
        }
    }
}
