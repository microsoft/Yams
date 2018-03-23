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

        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(1);

        public AzureTableUpdateSessionManagerTests(AzureStorageEmulatorTestFixture fixture)
        {
            fixture.ClearBlobStorage();

            CloudTableClient client = CloudStorageAccount.Parse(EmulatorConnectionString).CreateCloudTableClient();
            var table = client.GetTableReference(AzureTableUpdateSessionManager.UpdateSessionTableName);
            table.DeleteIfExists();

            _updateSessionManager = CreateUpdateSessionManager("superClusterId", "instanceId1", "1");
        }

        [Fact]
        public async Task TestStartUpdateSessionSimple()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession());
        }

        [Fact]
        public async Task TestThatOnlyOneUpdateDomainCanUpdateAtATime()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession());
            IUpdateSessionManager otherUpdateSessionManager = CreateUpdateSessionManager("superClusterId", "instanceId2", "2");
            Assert.False(await otherUpdateSessionManager.TryStartUpdateSession());
        }

        private static IUpdateSessionManager CreateUpdateSessionManager(string superClusterId, string instanceId, string updateDomain)
        {
            var updateSessionTable = new UpdateSessionTable(EmulatorConnectionString, ttl: TimeSpan.FromMinutes(1));
            return new AzureTableUpdateSessionManager(updateSessionTable, superClusterId, instanceId, updateDomain);
        }

        [Fact]
        public async Task TestThatMultipleInstancesInTheSameUpdateDomainCanUpdateSimultaneously()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession());

            IUpdateSessionManager otherUpdateSessionManager = CreateUpdateSessionManager("superClusterId", "instanceId2", "1");
            Assert.True(await otherUpdateSessionManager.TryStartUpdateSession());
        }

        [Fact]
        public async Task TestThatEndUpdateSessionWorks()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession());

            await _updateSessionManager.EndUpdateSession();

            IUpdateSessionManager otherUpdateSessionManager = CreateUpdateSessionManager("superClusterId", "instanceId2", "2");
            Assert.True(await otherUpdateSessionManager.TryStartUpdateSession());
        }

        [Fact]
        public async Task TestThatDifferentDeploymentsCanUpdateIndependently()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession());

            IUpdateSessionManager otherUpdateSessionManager = CreateUpdateSessionManager("superClusterId2", "instanceId2", "2");
            Assert.True(await otherUpdateSessionManager.TryStartUpdateSession());
        }

        [Fact]
        public async Task TestThatMultipleAppsCanUpdateSimultaneously()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession());
            Assert.True(await _updateSessionManager.TryStartUpdateSession());
        }

        [Fact]
        public async Task TestEdgeCase_InstanceEnlists_SetUpdateDomain_Race()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession());
            await _updateSessionManager.EndUpdateSession();

            IUpdateSessionTable updateSessionTable = new UpdateSessionTable(EmulatorConnectionString, Ttl);

            var updateSessionTableStub = ReplaceExecuteTransactionImplementation(updateSessionTable,
                async transaction =>
                {
                    // enlist another instance right before changing the update domain
                    await _updateSessionManager.TryStartUpdateSession();
                    return await updateSessionTable.TryExecuteTransaction(transaction);
                });

            var updateSessionManager = new AzureTableUpdateSessionManager(updateSessionTableStub, "superClusterId", "instanceId2", "2");
            Assert.False(await updateSessionManager.TryStartUpdateSession());
        }

        [Fact]
        public async Task TestEdgeCase_InstanceEnlists_InsertUpdateDomain_Race()
        {
            var updateSessionTable = new UpdateSessionTable(EmulatorConnectionString, Ttl);

            var updateSessionTableStub = ReplaceExecuteTransactionImplementation(updateSessionTable,
                async transaction =>
                {
                    // enlist another instance right before changing the update domain
                    await _updateSessionManager.TryStartUpdateSession();
                    return await updateSessionTable.TryExecuteTransaction(transaction);
                });

            var updateSessionManager = new AzureTableUpdateSessionManager(updateSessionTableStub, "superClusterId", "instanceId2", "2");
            Assert.False(await updateSessionManager.TryStartUpdateSession());
        }

        [Fact]
        public async Task TestEdgeCase_SetUpdateDomainRace()
        {
            Assert.True(await _updateSessionManager.TryStartUpdateSession());
            await _updateSessionManager.EndUpdateSession();

            var updateSessionTable = new UpdateSessionTable(EmulatorConnectionString, Ttl);

            var updateSessionTableStub = ReplaceExecuteTransactionImplementation(updateSessionTable,
                async transaction =>
                {
                    // set update domain 3 right before update domain 2 is being set
                    var updateSession =
                        new AzureTableUpdateSessionManager(updateSessionTable, "superClusterId", "instanceId3", "3");
                    Assert.True(await updateSession.TryStartUpdateSession());

                    return await updateSessionTable.TryExecuteTransaction(transaction);
                });

            var updateSessionManager = new AzureTableUpdateSessionManager(updateSessionTableStub, "superClusterId", "instanceId2", "2");
            Assert.False(await updateSessionManager.TryStartUpdateSession());
        }

        [Fact]
        public async Task TestEdgeCase_InsertUpdateDomainRace()
        {
            var updateSessionTable = new UpdateSessionTable(EmulatorConnectionString, Ttl);

            var updateSessionTableStub = ReplaceExecuteTransactionImplementation(updateSessionTable,
                async transaction =>
                {
                    Assert.True(await _updateSessionManager.TryStartUpdateSession());
                    return await updateSessionTable.TryExecuteTransaction(transaction);
                });

            var updateSessionManager = new AzureTableUpdateSessionManager(updateSessionTableStub, "superClusterId", "instanceId2", "2");
            Assert.False(await updateSessionManager.TryStartUpdateSession());
        }

        [Fact]
        public async Task TestThatStorageExceptionsAreRetried()
        {
            var fetchUpdateSessionStatusStub = StubsUtils.Sequence<Func<Task<UpdateSessionStatus>>>()
                .Once(() => AsyncUtils.AsyncTaskThatThrows<UpdateSessionStatus>(new StorageException()))
                .Once(() => Task.FromResult(new UpdateSessionStatus(Enumerable.Empty<UpdateDomainEntity>(), null, null)));

            IUpdateSessionTable updateSessionTableStub = new StubIUpdateSessionTable()
                .FetchUpdateSessionStatus((superClusterId) => fetchUpdateSessionStatusStub.Next())
                .TryExecuteTransaction(transaction => Task.FromResult(true))
                .DeleteInstanceEntity((superClusterId, instanceId) => Task.FromResult(true))
                .GetActiveUpdateDomain((superClusterId) => Task.FromResult("1"));

            ContainerBuilder builder = AzureStorageUpdateSessionDiModule.RegisterTypes("superClusterId", "superClusterId",
                "instanceId", "1", EmulatorConnectionString, Ttl);
            builder.RegisterInstance(updateSessionTableStub);
            IUpdateSessionManager updateSessionManager = new AzureStorageUpdateSessionDiModule(builder.Build())
                .UpdateSessionManager;
            Assert.True(await updateSessionManager.TryStartUpdateSession());
            
            await updateSessionManager.EndUpdateSession();

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
