using System;
using System.Threading.Tasks;
using Etg.Yams.Azure.Lease;
using Etg.Yams.Azure.UpdateSession;
using Etg.Yams.Azure.Utils;
using Etg.Yams.AzureTestUtils.Fixtures;
using Etg.Yams.TestUtils;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Xunit;

namespace Etg.Yams.Azure.Test.UpdateSession
{
    public class UpdateBlobTest : IClassFixture<AzureStorageEmulatorTestFixture>
    {
        private readonly UpdateBlobFactory _updateBlobFactory;
        private readonly CloudBlobContainer _container;

        public UpdateBlobTest(AzureStorageEmulatorTestFixture fixture)
        {
            fixture.ClearBlobStorage();
            var blobClient = fixture.BlobClient;
            const string containerName = "container";
            _container = blobClient.GetContainerReference(containerName);
            _container.CreateIfNotExists();
            _updateBlobFactory = new UpdateBlobFactory("clusterId", _container, new BlobLeaseFactory());
        }

        [Fact]
        public async Task TestSetDataWhenBlobIsEmpty()
        {
            IUpdateBlob updateBlob = await _updateBlobFactory.TryLockUpdateBlob("appId");

            const string instanceId = "instanceId";
            const string updateDomain = "1";
            updateBlob.SetUpdateDomain(updateDomain);
            updateBlob.AddInstance(instanceId);
            await updateBlob.FlushAndRelease();

            await updateBlob.TryLock();
            Assert.Equal(updateDomain, updateBlob.GetUpdateDomain());
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { instanceId }, updateBlob.GetInstanceIds());
        }

        [Fact]
        public async Task TestAddingMultipleInstanceIds()
        {
            IUpdateBlob updateBlob = await _updateBlobFactory.TryLockUpdateBlob("appId");

            updateBlob.SetUpdateDomain("1");
            updateBlob.AddInstance("instanceId1");
            updateBlob.AddInstance("instanceId2");
            await updateBlob.FlushAndRelease();

            await updateBlob.TryLock();
            Assert.Equal("1", updateBlob.GetUpdateDomain());
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "instanceId1", "instanceId2" }, updateBlob.GetInstanceIds());
        }

        [Fact]
        public async Task TestRemoveInstanceId()
        {
            IUpdateBlob updateBlob = await _updateBlobFactory.TryLockUpdateBlob("appId");

            updateBlob.SetUpdateDomain("1");
            updateBlob.AddInstance("instanceId1");
            updateBlob.AddInstance("instanceId2");
            await updateBlob.FlushAndRelease();

            updateBlob = await _updateBlobFactory.TryLockUpdateBlob("appId");
            updateBlob.RemoveInstance("instanceId1");
            await updateBlob.FlushAndRelease();

            await updateBlob.TryLock();
            Assert.Equal("1", updateBlob.GetUpdateDomain());
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "instanceId2" }, updateBlob.GetInstanceIds());
            await updateBlob.Release();

            updateBlob = await _updateBlobFactory.TryLockUpdateBlob("appId");
            updateBlob.RemoveInstance("instanceId2");
            await updateBlob.FlushAndRelease();

            await updateBlob.TryLock();
            Assert.True(string.IsNullOrEmpty(updateBlob.GetUpdateDomain()));
            Assert.Empty(updateBlob.GetInstanceIds());
        }

        [Fact]
        public async Task TestGetUpdateDomainOnEmptyBlob()
        {
            IUpdateBlob updateBlob = await _updateBlobFactory.TryLockUpdateBlob("appId");
            Assert.True(string.IsNullOrEmpty(updateBlob.GetUpdateDomain()));
        }

        [Fact]
        public async Task TestGetInstanceIdsOnEmptyBlob()
        {
            IUpdateBlob updateBlob = await _updateBlobFactory.TryLockUpdateBlob("appId");
            Assert.Empty(updateBlob.GetInstanceIds());
        }

        [Fact]
        public async Task TestThatUpdateBlobMustBeLockedBeforeAnyOperation()
        {
            IUpdateBlob updateBlob = await _updateBlobFactory.TryLockUpdateBlob("appId");
            await updateBlob.FlushAndRelease();
            Assert.Throws<InvalidOperationException>(() => updateBlob.GetUpdateDomain());
            Assert.Throws<InvalidOperationException>(() => updateBlob.GetInstanceIds());
            Assert.Throws<InvalidOperationException>(() => updateBlob.AddInstance("1"));
            Assert.Throws<InvalidOperationException>(() => updateBlob.RemoveInstance("1"));
            Assert.Throws<InvalidOperationException>(() => updateBlob.SetUpdateDomain("1"));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await updateBlob.FlushAndRelease());
        }

        [Fact]
        public async Task TestFlushWhenInstanceAreSetButNoUpdateDomain()
        {
            IUpdateBlob updateBlob = await _updateBlobFactory.TryLockUpdateBlob("appId");
            updateBlob.AddInstance("instanceId1");
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await updateBlob.FlushAndRelease());
        }

        [Fact]
        public async Task TestThatDisposeReleasesTheLease()
        {
            IUpdateBlob updateBlob = await _updateBlobFactory.TryLockUpdateBlob("appId");
            updateBlob.Dispose();
            Assert.NotNull(await _updateBlobFactory.TryLockUpdateBlob("appId"));
        }

        [Fact]
        public async Task TestThatLockFailsIfBlobLeaseReturnsNull()
        {
            var blobLeaseStub = new StubIBlobLease()
                .TryAcquireLease(() => AsyncUtils.AsyncTaskWithResult<string>(null))
                .Dispose(() => { }
            );
            await TestThatLockFailsIfBlobLeaseCantBeAcquired(blobLeaseStub);
        }

        [Fact]
        public async Task TestThatLockFailsIfBlobLeaseThrowsStorageException()
        {
            var blobLeaseStub = new StubIBlobLease()
                .TryAcquireLease (() => AsyncUtils.AsyncTaskThatThrows<string>(new StorageException()))
                .Dispose(() => { }
            );
            await TestThatLockFailsIfBlobLeaseCantBeAcquired(blobLeaseStub);
        }

        private async Task TestThatLockFailsIfBlobLeaseCantBeAcquired(IBlobLease blobLeaseStub)
        {

	        var leaseFactoryMock = new StubIBlobLeaseFactory()
		        .CreateLease(blob => blobLeaseStub);

            var testBlob = _container.GetBlockBlobReference("testBlob");
            await BlobUtils.CreateEmptyBlob(testBlob);
            UpdateBlob updateBlob = new UpdateBlob(testBlob, leaseFactoryMock);
            Assert.False(await updateBlob.TryLock());
        }
    }
}
