using System.Threading.Tasks;
using Autofac;
using Etg.Yams.Azure.UpdateSession;
using Etg.Yams.AzureTestUtils.Fixtures;
using Etg.Yams.Update;
using Microsoft.WindowsAzure.Storage;
using Moq;
using Xunit;

namespace Etg.Yams.Azure.Test.UpdateSession
{
    public class EndToEndTest : IClassFixture<AzureStorageEmulatorTestFixture>
    {
        private const string EmulatorConnectionString = "UseDevelopmentStorage=true";
        private readonly IUpdateSessionManager _updateSessionManager;

        public EndToEndTest(AzureStorageEmulatorTestFixture fixture)
        {
            fixture.ClearBlobStorage();

            var module = new AzureBlobStorageUpdateSessionDiModule("deploymentId", "instanceId", "1",
                EmulatorConnectionString);
            _updateSessionManager = module.UpdateSessionManager;
        }

        [Fact]
        public async Task TestStartUpdateSessionSimple()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession("app1"));
        }

        [Fact]
        public async Task TestThatOnlyOneUpdateDomainCanUpdateAtATime()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession("app1"));
            IUpdateSessionManager otherUpdateSessionManager = CreateUpdateSessionManager("deploymentId", "instanceId2", "2");
            Assert.False(await otherUpdateSessionManager.TryStartUpdateSession("app1"));
        }

        private static IUpdateSessionManager CreateUpdateSessionManager(string deploymentId, string instanceId, string updateDomain)
        {
            return new AzureBlobStorageUpdateSessionDiModule(deploymentId, instanceId, updateDomain, EmulatorConnectionString).UpdateSessionManager;
        }

        [Fact]
        public async Task TestThatMultipleInstancesInTheSameUpdateDomainCanUpdateSimultaneously()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession("app1"));

            IUpdateSessionManager otherUpdateSessionManager = CreateUpdateSessionManager("deploymentId", "instanceId2", "1");
            Assert.True(await otherUpdateSessionManager.TryStartUpdateSession("app1"));
        }

        [Fact]
        public async Task TestThatEndUpdateSessionWorks()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession("app1"));

            await _updateSessionManager.EndUpdateSession("app1");

            IUpdateSessionManager otherUpdateSessionManager = CreateUpdateSessionManager("deploymentId", "instanceId2", "2");
            Assert.True(await otherUpdateSessionManager.TryStartUpdateSession("app1"));
        }

        [Fact]
        public async Task TestThatDifferentClustersCanUpdateIndependently()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession("app1"));

            IUpdateSessionManager otherUpdateSessionManager = CreateUpdateSessionManager("deploymentId2", "instanceId2", "2");
            Assert.True(await otherUpdateSessionManager.TryStartUpdateSession("app1"));
        }

        [Fact]
        public async Task TestThatStorageExceptionsAreRetried()
        {
            string appId = "appId";
            var updateBlobMock = new Mock<IUpdateBlob>();
            updateBlobMock.SetupSequence(blob => blob.FlushAndRelease())
                .Throws(new StorageException())
                .Returns(Task.CompletedTask);
            var updateBlobFactoryMock = new Mock<IUpdateBlobFactory>();
            updateBlobFactoryMock.Setup(factory => factory.TryLockUpdateBlob(appId)).ReturnsAsync(updateBlobMock.Object);

            ContainerBuilder builder = AzureBlobStorageUpdateSessionDiModule.RegisterTypes("deploymentId",
                "instanceId", "1",
                EmulatorConnectionString);
            builder.RegisterInstance(updateBlobFactoryMock.Object);
            IUpdateSessionManager updateSessionManager = new AzureBlobStorageUpdateSessionDiModule(builder.Build()).UpdateSessionManager;
            Assert.True(await updateSessionManager.TryStartUpdateSession(appId));

            updateBlobMock.SetupSequence(blob => blob.FlushAndRelease())
                .Throws(new StorageException())
                .Returns(Task.CompletedTask);
            await updateSessionManager.EndUpdateSession(appId);

            updateBlobMock.Verify(blob => blob.FlushAndRelease(), Times.Exactly(4));
        }
    }
}
