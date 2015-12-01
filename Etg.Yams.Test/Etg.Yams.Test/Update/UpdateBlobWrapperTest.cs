using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureTestUtils;
using Etg.Yams.Update;
using Etg.Yams.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Test.Update
{
    [TestClass]
    public class UpdateBlobWrapperTest
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
        public async Task TestSetDataWhenBlobIsEmpty()
        {
            ICloudBlob blob = await CreateEmptyBlob();
            string leaseId = blob.AcquireLease(null, null);
            UpdateBlobWrapper updateBlobWrapper = await UpdateBlobWrapper.Create(blob);
            
            const string instanceId = "instanceId";
            const string updateDomain = "1";
            await updateBlobWrapper.SetData(leaseId, updateDomain, new[] {instanceId});

            Assert.AreEqual(updateDomain, updateBlobWrapper.GetUpdateDomain());
            Assert.IsTrue(new HashSet<string>(new[]{instanceId}).SetEquals(updateBlobWrapper.GetInstanceIds()));
        }

        [TestMethod]
        public async Task TestThatSetDataOverwritesExistingContent()
        {
            ICloudBlob blob = await CreateEmptyBlob();
            string leaseId = blob.AcquireLease(null, null);
            UpdateBlobWrapper updateBlobWrapper = await UpdateBlobWrapper.Create(blob);
            
            await updateBlobWrapper.SetData(leaseId, "1", new[] { "instanceId1" });
            await updateBlobWrapper.SetData(leaseId, "2", new[] { "instanceId2" });

            Assert.AreEqual("2", updateBlobWrapper.GetUpdateDomain());
            Assert.IsTrue(new HashSet<string>(new[] { "instanceId2" }).SetEquals(updateBlobWrapper.GetInstanceIds()));
        }

        [TestMethod]
        public async Task TestSetDataWithMultipleInstanceIds()
        {
            ICloudBlob blob = await CreateEmptyBlob();
            string leaseId = blob.AcquireLease(null, null);
            UpdateBlobWrapper updateBlobWrapper = await UpdateBlobWrapper.Create(blob);
            
            var instanceIds = new HashSet<string>(new[] {"instanceId1"});
            await updateBlobWrapper.SetData(leaseId, "1", instanceIds);

            Assert.AreEqual("1", updateBlobWrapper.GetUpdateDomain());
            Assert.IsTrue(instanceIds.SetEquals(updateBlobWrapper.GetInstanceIds()));
        }

        [TestMethod]
        public async Task TestSetDataWithEmptySetOfInstanceIds()
        {
            ICloudBlob blob = await CreateEmptyBlob();
            string leaseId = blob.AcquireLease(null, null);
            UpdateBlobWrapper updateBlobWrapper = await UpdateBlobWrapper.Create(blob);
            
            var instanceIds = new HashSet<string>(new[] { "instanceId1" });
            await updateBlobWrapper.SetData(leaseId, "1", instanceIds);
            await updateBlobWrapper.SetData(leaseId, "1", new HashSet<string>());

            Assert.AreEqual("1", updateBlobWrapper.GetUpdateDomain());
            Assert.IsFalse(updateBlobWrapper.GetInstanceIds().Any());
        }

        [TestMethod]
        public async Task TestGetUpdateDomainOnEmptyBlob()
        {
            ICloudBlob blob = await CreateEmptyBlob();
            blob.AcquireLease(null, null);
            UpdateBlobWrapper updateBlobWrapper = await UpdateBlobWrapper.Create(blob);

            Assert.AreEqual(0, updateBlobWrapper.GetUpdateDomain().Length);
        }

        [TestMethod]
        public async Task TestGetInstanceIdsOnEmptyBlob()
        {
            ICloudBlob blob = await CreateEmptyBlob();
            blob.AcquireLease(null, null);
            UpdateBlobWrapper updateBlobWrapper = await UpdateBlobWrapper.Create(blob);

            Assert.AreEqual(0, updateBlobWrapper.GetInstanceIds().Count);
        }

        [TestMethod]
        [ExpectedException(typeof(StorageException))]
        public async Task TestThatSetDataFailsIfWrongLeaseIdIsGiven()
        {
            ICloudBlob blob = await CreateEmptyBlob();
            blob.AcquireLease(null, null);
            UpdateBlobWrapper updateBlobWrapper = await UpdateBlobWrapper.Create(blob);

            var instanceIds = new HashSet<string>(new[] { "instanceId1" });
            await updateBlobWrapper.SetData("wrongLeaseId", "1", instanceIds);
        }

        [TestMethod]
        public async Task TestThatConstructorCreatesABlobIfNoneExists()
        {
            ICloudBlob blob = await CreateEmptyBlob();
            blob.Delete();

            await UpdateBlobWrapper.Create(blob);
            Assert.IsTrue(blob.Exists());
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
