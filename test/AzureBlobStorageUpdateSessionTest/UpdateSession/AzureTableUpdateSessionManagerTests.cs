using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Etg.SimpleStubs;
using Etg.Yams.Azure.UpdateSession;
using Etg.Yams.AzureTestUtils.Fixtures;
using Etg.Yams.TestUtils;
using Etg.Yams.Update;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Xunit;

namespace Etg.Yams.Azure.Test.UpdateSession
{
    public class AzureTableUpdateSessionManagerTests : IClassFixture<AzureStorageEmulatorTestFixture>
    {
        private const string EmulatorConnectionString = "UseDevelopmentStorage=true";
        private readonly IUpdateSessionManager _updateSessionManager;

        public AzureTableUpdateSessionManagerTests(AzureStorageEmulatorTestFixture fixture)
        {
            fixture.ClearBlobStorage();

            CloudTableClient client = CloudStorageAccount.Parse(EmulatorConnectionString).CreateCloudTableClient();
            var table = client.GetTableReference(AzureTableUpdateSessionManager.UpdateSessionTableName);
            table.DeleteIfExists();

            _updateSessionManager = CreateUpdateSessionManager("clusterId", "instanceId1", "1");
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
            var updateSessionTable = new UpdateSessionTable(EmulatorConnectionString, ttl:TimeSpan.FromMinutes(1));
            return new AzureTableUpdateSessionManager(updateSessionTable, clusterId, instanceId, updateDomain);
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
        public async Task TestThatMultipleAppsCanUpdateSimultaneously()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession("app1"));
            Assert.True(await _updateSessionManager.TryStartUpdateSession("app2"));
        }

        [Fact]
        public async Task TestThatDiffentAppsCanUpdateIndependently()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession("app1"));
            IUpdateSessionManager otherUpdateSessionManager = CreateUpdateSessionManager("clusterId1", "instanceId2", "2");
            Assert.True(await otherUpdateSessionManager.TryStartUpdateSession("app2"));
        }

        [Fact]
        public async Task TestEdgeCase_InstanceEnlists_SetUpdateDomain_Race()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession("app1"));
            await _updateSessionManager.EndUpdateSession("app1");

            IUpdateSessionTable updateSessionTable = new UpdateSessionTable(EmulatorConnectionString, ttl: TimeSpan.FromMinutes(15));

            var updateSessionTableStub = ReplaceExecuteTransactionImplementation(updateSessionTable,
                async transaction =>
                {
                    // enlist another instance right before changing the update domain
                    await _updateSessionManager.TryStartUpdateSession("app1");
                    return await updateSessionTable.TryExecuteTransaction(transaction);
                });

            var updateSessionManager = new AzureTableUpdateSessionManager(updateSessionTableStub, "clusterId", "instanceId2", "2");
            Assert.False(await updateSessionManager.TryStartUpdateSession("app1"));
        }

        [Fact]
        public async Task TestEdgeCase_InstanceEnlists_InsertUpdateDomain_Race()
        {
            var updateSessionTable = new UpdateSessionTable(EmulatorConnectionString, ttl: TimeSpan.FromMinutes(15));

            var updateSessionTableStub = ReplaceExecuteTransactionImplementation(updateSessionTable,
                async transaction =>
                {
                    // enlist another instance right before changing the update domain
                    await _updateSessionManager.TryStartUpdateSession("app1");
                    return await updateSessionTable.TryExecuteTransaction(transaction);
                });

            var updateSessionManager = new AzureTableUpdateSessionManager(updateSessionTableStub, "clusterId", "instanceId2", "2");
            Assert.False(await updateSessionManager.TryStartUpdateSession("app1"));
        }

        [Fact]
        public async Task TestEdgeCase_SetUpdateDomainRace()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession("app1"));
            await _updateSessionManager.EndUpdateSession("app1");

            var updateSessionTable = new UpdateSessionTable(EmulatorConnectionString, ttl: TimeSpan.FromMinutes(15));

            var updateSessionTableStub = ReplaceExecuteTransactionImplementation(updateSessionTable,
                async transaction =>
                {
                    // set update domain 3 right before update domain 2 is being set
                    var updateSession =
                        new AzureTableUpdateSessionManager(updateSessionTable, "clusterId", "instanceId3", "3");
                    Assert.True(await updateSession.TryStartUpdateSession("app1"));

                    return await updateSessionTable.TryExecuteTransaction(transaction);
                });

            var updateSessionManager = new AzureTableUpdateSessionManager(updateSessionTableStub, "clusterId", "instanceId2", "2");
            Assert.False(await updateSessionManager.TryStartUpdateSession("app1"));
        }

        [Fact]
        public async Task TestEdgeCase_InsertUpdateDomainRace()
        {
            var updateSessionTable = new UpdateSessionTable(EmulatorConnectionString, ttl: TimeSpan.FromMinutes(15));

            var updateSessionTableStub = ReplaceExecuteTransactionImplementation(updateSessionTable,
                async transaction =>
                {
                    Assert.True(await _updateSessionManager.TryStartUpdateSession("app1"));
                    return await updateSessionTable.TryExecuteTransaction(transaction);
                });

            var updateSessionManager = new AzureTableUpdateSessionManager(updateSessionTableStub, "clusterId", "instanceId2", "2");
            Assert.False(await updateSessionManager.TryStartUpdateSession("app1"));
        }

        [Fact]
        public async Task TestThatStorageExceptionsAreRetried()
        {
            string appId = "appId";

            var fetchUpdateSessionStatusStub = StubsUtils.Sequence<Func<Task<UpdateSessionStatus>>>()
                .Once(() => AsyncUtils.AsyncTaskThatThrows<UpdateSessionStatus>(new StorageException()))
                .Once(() => Task.FromResult(new UpdateSessionStatus(Enumerable.Empty<UpdateDomainEntity>(), null, null)));

            IUpdateSessionTable updateSessionTableStub = new StubIUpdateSessionTable()
                .FetchUpdateSessionStatus((clusterId, app) => fetchUpdateSessionStatusStub.Next())
                .TryExecuteTransaction(transaction => Task.FromResult(true))
                .DeleteInstanceEntity((clusterId, instanceId, app) => Task.FromResult(true))
                .GetActiveUpdateDomain((clusterId, app) => Task.FromResult("1"));

            ContainerBuilder builder = AzureStorageUpdateSessionDiModule.RegisterTypes("clusterId",
                "instanceId", "1", EmulatorConnectionString, TimeSpan.FromMinutes(1));
            builder.RegisterInstance(updateSessionTableStub);
            IUpdateSessionManager updateSessionManager = new AzureStorageUpdateSessionDiModule(builder.Build())
                .UpdateSessionManager;
            Assert.True(await updateSessionManager.TryStartUpdateSession(appId));
            
            await updateSessionManager.EndUpdateSession(appId);

            Assert.Equal(2, fetchUpdateSessionStatusStub.CallCount);
        }

        private IUpdateSessionTable ReplaceExecuteTransactionImplementation(IUpdateSessionTable updateSessionTable,
            StubIUpdateSessionTable.TryExecuteTransaction_UpdateSessionTransaction_Delegate del)
        {
            IUpdateSessionTable updateSessionTableStub = new StubIUpdateSessionTable()
                .FetchUpdateSessionStatus(updateSessionTable.FetchUpdateSessionStatus)
                .TryExecuteTransaction(del)
                .DeleteInstanceEntity(updateSessionTable.DeleteInstanceEntity)
                .GetActiveUpdateDomain(updateSessionTable.GetActiveUpdateDomain);
            return updateSessionTableStub;
        }
    }
}
