using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureTestUtils;
using Etg.Yams.Update;
using Etg.Yams.Utils;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using Xunit;

namespace Etg.Yams.Test.Update
{
    public class UpdateBlobWrapperTestFixture : IDisposable
    {
        private CloudStorageAccount _account;
        public CloudBlobClient BlobClient;
        public StorageEmulatorProxy StorageEmulatorProxy;

        public UpdateBlobWrapperTestFixture()
        {
            _account = CloudStorageAccount.DevelopmentStorageAccount;
            BlobClient = _account.CreateCloudBlobClient();

            StorageEmulatorProxy = new StorageEmulatorProxy();
            StorageEmulatorProxy.StartEmulator();
        }

        public void Dispose()
        {
            StorageEmulatorProxy.StopEmulator();
        }
    }


    public class UpdateBlobWrapperTest : IClassFixture<UpdateBlobWrapperTestFixture>
    {
        //private static CloudStorageAccount _account;
        private static CloudBlobClient _blobClient;
        //private static StorageEmulatorProxy _storageEmulatorProxy;


        public void CleanAndRestartStorage(UpdateBlobWrapperTestFixture fixture)
        {
            fixture.StorageEmulatorProxy.ClearBlobStorage();
            _blobClient = fixture.BlobClient;
        }

        [Fact]
        public async Task TestSetDataWhenBlobIsEmpty()
        {
            ICloudBlob blob = await CreateEmptyBlob();
            string leaseId = blob.AcquireLease(null, null);
            UpdateBlobWrapper updateBlobWrapper = await UpdateBlobWrapper.Create(blob);

            const string instanceId = "instanceId";
            const string updateDomain = "1";
            await updateBlobWrapper.SetData(leaseId, updateDomain, new[] { instanceId });

            Assert.Equal(updateDomain, updateBlobWrapper.GetUpdateDomain());
            Assert.True(new HashSet<string>(new[] { instanceId }).SetEquals(updateBlobWrapper.GetInstanceIds()));
        }

        [Fact]
        public async Task TestThatSetDataOverwritesExistingContent()
        {
            ICloudBlob blob = await CreateEmptyBlob();
            string leaseId = blob.AcquireLease(null, null);
            UpdateBlobWrapper updateBlobWrapper = await UpdateBlobWrapper.Create(blob);

            await updateBlobWrapper.SetData(leaseId, "1", new[] { "instanceId1" });
            await updateBlobWrapper.SetData(leaseId, "2", new[] { "instanceId2" });

            Assert.Equal("2", updateBlobWrapper.GetUpdateDomain());
            Assert.True(new HashSet<string>(new[] { "instanceId2" }).SetEquals(updateBlobWrapper.GetInstanceIds()));
        }

        [Fact]
        public async Task TestSetDataWithMultipleInstanceIds()
        {
            ICloudBlob blob = await CreateEmptyBlob();
            string leaseId = blob.AcquireLease(null, null);
            UpdateBlobWrapper updateBlobWrapper = await UpdateBlobWrapper.Create(blob);

            var instanceIds = new HashSet<string>(new[] { "instanceId1" });
            await updateBlobWrapper.SetData(leaseId, "1", instanceIds);

            Assert.Equal("1", updateBlobWrapper.GetUpdateDomain());
            Assert.True(instanceIds.SetEquals(updateBlobWrapper.GetInstanceIds()));
        }

        [Fact]
        public async Task TestSetDataWithEmptySetOfInstanceIds()
        {
            ICloudBlob blob = await CreateEmptyBlob();
            string leaseId = blob.AcquireLease(null, null);
            UpdateBlobWrapper updateBlobWrapper = await UpdateBlobWrapper.Create(blob);

            var instanceIds = new HashSet<string>(new[] { "instanceId1" });
            await updateBlobWrapper.SetData(leaseId, "1", instanceIds);
            await updateBlobWrapper.SetData(leaseId, "1", new HashSet<string>());

            Assert.Equal("1", updateBlobWrapper.GetUpdateDomain());
            Assert.False(updateBlobWrapper.GetInstanceIds().Any());
        }

        [Fact]
        public async Task TestGetUpdateDomainOnEmptyBlob()
        {
            ICloudBlob blob = await CreateEmptyBlob();
            blob.AcquireLease(null, null);
            UpdateBlobWrapper updateBlobWrapper = await UpdateBlobWrapper.Create(blob);

            Assert.Equal(0, updateBlobWrapper.GetUpdateDomain().Length);
        }

        [Fact]
        public async Task TestGetInstanceIdsOnEmptyBlob()
        {
            ICloudBlob blob = await CreateEmptyBlob();
            blob.AcquireLease(null, null);
            UpdateBlobWrapper updateBlobWrapper = await UpdateBlobWrapper.Create(blob);

            Assert.Equal(0, updateBlobWrapper.GetInstanceIds().Count);
        }

        [Fact]
        public async Task TestThatSetDataFailsIfWrongLeaseIdIsGiven()
        {
            await Assert.ThrowsAnyAsync<StorageException>(async () =>
            {
                ICloudBlob blob = await CreateEmptyBlob();
                blob.AcquireLease(null, null);
                UpdateBlobWrapper updateBlobWrapper = await UpdateBlobWrapper.Create(blob);

                var instanceIds = new HashSet<string>(new[] { "instanceId1" });
                await updateBlobWrapper.SetData("wrongLeaseId", "1", instanceIds);

            });
        }

        [Fact]
        public async Task TestThatConstructorCreatesABlobIfNoneExists()
        {
            ICloudBlob blob = await CreateEmptyBlob();
            blob.Delete();

            await UpdateBlobWrapper.Create(blob);
            Assert.True(blob.Exists());
        }

        private async Task<ICloudBlob> CreateEmptyBlob()
        {
            const string containerName = "container";
            CloudBlobContainer container = _blobClient.GetContainerReference(containerName);
            container.CreateIfNotExists();
            const string blobName = "updateBlob";
            ICloudBlob blob = container.GetBlockBlobReference(blobName);
            await BlobUtils.CreateEmptyBlob(blob);
            return blob;
        }
    }
}
