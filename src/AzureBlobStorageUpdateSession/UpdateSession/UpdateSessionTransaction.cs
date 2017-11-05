using Microsoft.WindowsAzure.Storage.Table;

namespace Etg.Yams.Azure.UpdateSession
{
    public class UpdateSessionTransaction
    {
        private readonly string _clusterId;
        private readonly string _instanceId;
        private readonly string _updateDomain;
        private readonly string _appId;
        private readonly TableBatchOperation _batchOperation = new TableBatchOperation();

        public UpdateSessionTransaction(string clusterId, string instanceId, string updateDomain, string appId)
        {
            _clusterId = clusterId;
            _instanceId = instanceId;
            _updateDomain = updateDomain;
            _appId = appId;
        }

        public UpdateSessionTransaction InsertUpdateDomain()
        {
            var updateDomainEntity = new UpdateDomainEntity(PartitionKey,
                UpdateSessionTable.UpdateDomainRowKey, _updateDomain);
            _batchOperation.Add(TableOperation.Insert(updateDomainEntity));
            return this;
        }

        public UpdateSessionTransaction InsertOrReplaceInstance()
        {
            var instanceEntity = new UpdateDomainEntity(PartitionKey, _instanceId, _updateDomain);
            _batchOperation.Add(TableOperation.InsertOrReplace(instanceEntity));

            return this;
        }

        public UpdateSessionTransaction MarkInstanceListAsModified()
        {
            var modifiedEntity = new UpdateDomainEntity(PartitionKey, UpdateSessionTable.ModifiedRowKey, "");
            _batchOperation.Add(TableOperation.InsertOrReplace(modifiedEntity));
            return this;
        }

        public UpdateSessionTransaction FailIfInstanceListModified(UpdateSessionStatus status)
        {
            _batchOperation.Add(TableOperation.Replace(status.ModifiedEntity));
            return this;
        }

        public UpdateSessionTransaction ReplaceUpdateDomain(UpdateSessionStatus status)
        {
            var updateDomainEntity = status.UpdateDomainEntity;
            updateDomainEntity.UpdateDomain = _updateDomain;
            _batchOperation.Add(TableOperation.Replace(updateDomainEntity));

            return this;
        }

        public TableBatchOperation BatchOperation => _batchOperation;

        private string PartitionKey => UpdateSessionTable.GetPartitionKey(_clusterId, _appId);
    }
}