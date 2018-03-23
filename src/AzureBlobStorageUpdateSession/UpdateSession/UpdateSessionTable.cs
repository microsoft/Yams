using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Etg.Yams.Azure.UpdateSession
{
    public class UpdateSessionTable : IUpdateSessionTable
    {
        private readonly CloudTable _table;
        private readonly TimeSpan _ttl;
        public const string UpdateSessionTableName = "YamsUpdateSession";
        public const string UpdateDomainRowKey = "updateDomain";
        public const string ModifiedRowKey = "modified";


        /// <summary>
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="ttl">If an instance update session is older than ttl, it will be ignored. The goal of this parameter 
        /// is to avoid situations where an instance fails to end its update session and blocks the cluster.</param>
        public UpdateSessionTable(string connectionString, TimeSpan ttl)
        {
            _ttl = ttl;
            CloudTableClient client = CloudStorageAccount.Parse(connectionString).CreateCloudTableClient();
            _table = client.GetTableReference(UpdateSessionTableName);
            _table.CreateIfNotExists();
        }

        public Task<UpdateSessionStatus> FetchUpdateSessionStatus(string superClusterId)
        {
            string partitionQuery = CreatePartitionQuery(superClusterId);

            TableQuery<UpdateDomainEntity> query = new TableQuery<UpdateDomainEntity>().Where(partitionQuery);
            UpdateDomainEntity updateDomainEntity = null;
            UpdateDomainEntity instanceListModifiedEntity = null;
            Dictionary<string, List<UpdateDomainEntity>> instanceEntitiesDict = new Dictionary<string, List<UpdateDomainEntity>>();
            foreach (UpdateDomainEntity entity in _table.ExecuteQuery(query))
            {
                if (entity.RowKey == UpdateDomainRowKey)
                {
                    updateDomainEntity = entity;
                }
                else if (entity.RowKey == ModifiedRowKey)
                {
                    instanceListModifiedEntity = entity;
                }
                else
                {
                    if (entity.Timestamp.Add(_ttl) > DateTimeOffset.Now)
                    {
                        AddInstanceEntity(instanceEntitiesDict, entity);
                    }
                }
            }

            List<UpdateDomainEntity> instancesEntities = null;
            string updateDomain = updateDomainEntity?.UpdateDomain;
            if (!string.IsNullOrWhiteSpace(updateDomain))
            {
                // filter out entities that do not match the active update domain (were leftover for some reason)
                instanceEntitiesDict.TryGetValue(updateDomain, out instancesEntities);
            }
            if (instancesEntities == null)
            {
                instancesEntities = new List<UpdateDomainEntity>();
            }

            return Task.FromResult(new UpdateSessionStatus(instancesEntities, instanceListModifiedEntity, updateDomainEntity));
        }

        private static void AddInstanceEntity(Dictionary<string, List<UpdateDomainEntity>> instanceEntitiesDict, UpdateDomainEntity entity)
        {
            List<UpdateDomainEntity> instancesList;
            if (instanceEntitiesDict.TryGetValue(entity.UpdateDomain, out instancesList))
            {
                instancesList.Add(entity);
            } 
            else
            {
                instancesList = new List<UpdateDomainEntity>();
                instancesList.Add(entity);
                instanceEntitiesDict[entity.UpdateDomain] = instancesList;
            }
        }

        public async Task<bool> TryExecuteTransaction(UpdateSessionTransaction transaction)
        {
            try
            {
                await _table.ExecuteBatchAsync(transaction.BatchOperation);

                return true;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode != 412  // insert operation of an entity that already exists
                    && e.RequestInformation.HttpStatusCode != 409 // optimistic concurrency conflict based on Etag
                    )
                {
                    throw;
                }
            }
            return false;
        }

        public async Task DeleteInstanceEntity(string superClusterId, string instanceId)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<UpdateDomainEntity>(superClusterId, 
                instanceId);
            TableResult retrievedResult = await _table.ExecuteAsync(retrieveOperation);
            var instanceEntity = (UpdateDomainEntity)retrievedResult.Result;
            if (instanceEntity != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(instanceEntity);
                await _table.ExecuteAsync(deleteOperation);
            }
        }

        public async Task<string> GetActiveUpdateDomain(string superClusterId)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<UpdateDomainEntity>(superClusterId, 
                UpdateDomainRowKey);

            // Execute the retrieve operation.
            TableResult retrievedResult = await _table.ExecuteAsync(retrieveOperation);

            var entity = (UpdateDomainEntity)retrievedResult?.Result;
            return entity?.UpdateDomain;
        }

        private static string CreatePartitionQuery(string superClusterId)
        {
            return TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                superClusterId);
        }
    }
}