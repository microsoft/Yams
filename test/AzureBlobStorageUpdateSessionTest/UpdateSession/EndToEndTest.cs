﻿using System;
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

            var module = new AzureBlobStorageUpdateSessionDiModule("clusterId", "instanceId", "1",
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
            IUpdateSessionManager otherUpdateSessionManager = CreateUpdateSessionManager("clusterId", "instanceId2", "2");
            Assert.False(await otherUpdateSessionManager.TryStartUpdateSession("app1"));
        }

        private static IUpdateSessionManager CreateUpdateSessionManager(string clusterId, string instanceId, string updateDomain)
        {
            return new AzureBlobStorageUpdateSessionDiModule(clusterId, instanceId, updateDomain, EmulatorConnectionString).UpdateSessionManager;
        }

        [Fact]
        public async Task TestThatMultipleInstancesInTheSameUpdateDomainCanUpdateSimultaneously()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession("app1"));

            IUpdateSessionManager otherUpdateSessionManager = CreateUpdateSessionManager("clusterId", "instanceId2", "1");
            Assert.True(await otherUpdateSessionManager.TryStartUpdateSession("app1"));
        }

        [Fact]
        public async Task TestThatEndUpdateSessionWorks()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession("app1"));

            await _updateSessionManager.EndUpdateSession("app1");

            IUpdateSessionManager otherUpdateSessionManager = CreateUpdateSessionManager("clusterId", "instanceId2", "2");
            Assert.True(await otherUpdateSessionManager.TryStartUpdateSession("app1"));
        }

        [Fact]
        public async Task TestThatDifferentClustersCanUpdateIndependently()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession("app1"));

            IUpdateSessionManager otherUpdateSessionManager = CreateUpdateSessionManager("clusterId2", "instanceId2", "2");
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

	        IUpdateBlob updateBlobStub = new StubIUpdateBlob()
		        .FlushAndRelease(() => flushAndReleaseSequence.Next())
		        .Dispose(() => { })
		        .GetUpdateDomain(() => "1")
		        .SetUpdateDomain(domain => { })
		        .AddInstance(id => { })
		        .RemoveInstance(id => { });

	        var updateBlobFactoryStub = new StubIUpdateBlobFactory()
		        .TryLockUpdateBlob(id => AsyncUtils.AsyncTaskWithResult(updateBlobStub));


            ContainerBuilder builder = AzureBlobStorageUpdateSessionDiModule.RegisterTypes("clusterId",
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
