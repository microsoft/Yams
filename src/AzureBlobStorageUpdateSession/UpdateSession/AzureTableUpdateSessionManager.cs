using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Update;

namespace Etg.Yams.Azure.UpdateSession
{
    public class AzureTableUpdateSessionManager : IUpdateSessionManager
    {
        public const string UpdateSessionTableName = "YamsUpdateSession";
        private readonly IUpdateSessionTable _updateSessionTable;
        private readonly string _clusterId;
        private readonly string _instanceId;
        private readonly string _instanceUpdateDomain;

        public AzureTableUpdateSessionManager(IUpdateSessionTable updateSessionTable, string clusterId, 
            string instanceId, string instanceUpdateDomain)
        {
            _updateSessionTable = updateSessionTable;
            _clusterId = clusterId;
            _instanceId = instanceId;
            _instanceUpdateDomain = instanceUpdateDomain;
        }

        public async Task<bool> TryStartUpdateSession(string appId)
        {
            Trace.TraceInformation(
                $"Instance {_instanceId} will attempt to start update session for " +
                $"ApplicationId = {appId}, UpdateDomain = {_instanceUpdateDomain}");

            UpdateSessionTransaction transaction = new UpdateSessionTransaction(_clusterId, _instanceId, _instanceUpdateDomain, appId);
            UpdateSessionStatus updateSessionStatus = await _updateSessionTable.FetchUpdateSessionStatus(_clusterId, appId);

            if (updateSessionStatus.UpdateDomainEntity == null ||
                updateSessionStatus.UpdateDomainEntity.UpdateDomain == _instanceUpdateDomain)
            {
                if (updateSessionStatus.UpdateDomainEntity == null)
                {
                    transaction.InsertUpdateDomain();
                }
                
                transaction.MarkInstanceListAsModified();
            }
            else if(!updateSessionStatus.InstancesEntities.Any()) // no instance in the current update domain is updating
            {
                // set a new update domain (if no other instance beats us to it)
                transaction.ReplaceUpdateDomain(updateSessionStatus); // will fail if current update domain changes
                transaction.FailIfInstanceListModified(updateSessionStatus); // will fail if instance list changes
            }
            else
            {
                return false;
            }

            // enlist the current instance (this will succeed even if the active update domain is different but we 
            // won't start the update session, see below)
            transaction.InsertOrReplaceInstance();

            if (await _updateSessionTable.TryExecuteTransaction(transaction))
            {
                // handle the case where an instance enlisted itself after the update domain has changed,
                string updateDomain = await _updateSessionTable.GetActiveUpdateDomain(_clusterId, appId);
                if (updateDomain != _instanceUpdateDomain)
                {
                    // Note that deleting this row is optional because it will filtered out anyway when list of instances
                    // of the active update domain is loaded (as a result, it's not an issue if this fails).
                    // We delete it anyway to keep the table clean.
                    await _updateSessionTable.DeleteInstanceEntity(_clusterId, _instanceId, appId);
                    return false;
                }
                Trace.TraceInformation(
                    $"Instance {_instanceId} successfully started the update session for " +
                    $"ApplicationId = {appId}, UpdateDomain = {_instanceUpdateDomain}");
                return true;
            }

            return false;
        }

        public async Task EndUpdateSession(string appId)
        {
            Trace.TraceInformation(
                $"Instance {_instanceId} Will attempt to end the update session for " +
                $"ApplicationId = {appId}, " +
                $"UpdateDomain = {_instanceUpdateDomain}");

            await _updateSessionTable.DeleteInstanceEntity(_clusterId, _instanceId, appId);

            Trace.TraceInformation(
                $"Instance {_instanceId} successfully ended the update session for " +
                $"ApplicationId = {appId}, " +
                $"UpdateDomain = {_instanceUpdateDomain}");

        }
    }
}