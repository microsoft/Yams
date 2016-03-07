using System;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using Autofac;
using Etg.SimpleStubs;
using Etg.Yams.Azure.UpdateSession;
using Etg.Yams.AzureTestUtils.Fixtures;
using Etg.Yams.TestUtils;
using Etg.Yams.Update;
using Microsoft.WindowsAzure.Storage;
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

            var flushAndReleaseSequence = StubsUtils.Sequence<Func<Task>>()
                .Once(() => AsyncUtils.AsyncTaskThatThrows(new StorageException()))
                .Once(() => Task.CompletedTask)
                .Once(() => AsyncUtils.AsyncTaskThatThrows(new StorageException()))
                .Once(() => Task.CompletedTask);

            IUpdateBlob updateBlobStub = new StubIUpdateBlob
            {
                FlushAndRelease = () => flushAndReleaseSequence.Next(),
                IDisposable_Dispose = () => { },
                GetUpdateDomain = () => "1",
                SetUpdateDomain_String = domain => { },
                AddInstance_String = id => { },
                RemoveInstance_String = id => { }
            };

            var updateBlobFactoryStub = new StubIUpdateBlobFactory
            {
                TryLockUpdateBlob_String = id => AsyncUtils.AsyncTaskWithResult(updateBlobStub)
            };


            ContainerBuilder builder = AzureBlobStorageUpdateSessionDiModule.RegisterTypes("deploymentId",
                "instanceId", "1",
                EmulatorConnectionString);
            builder.RegisterInstance(updateBlobFactoryStub).As<IUpdateBlobFactory>();
            IUpdateSessionManager updateSessionManager = new AzureBlobStorageUpdateSessionDiModule(builder.Build()).UpdateSessionManager;
            Assert.True(await updateSessionManager.TryStartUpdateSession(appId));
            
            await updateSessionManager.EndUpdateSession(appId);

            Assert.Equal(4, flushAndReleaseSequence.CallCount);
        }
    }
}
