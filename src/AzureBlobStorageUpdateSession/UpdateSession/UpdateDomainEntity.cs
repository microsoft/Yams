using Microsoft.WindowsAzure.Storage.Table;

namespace Etg.Yams.Azure.UpdateSession
{
    public class UpdateDomainEntity : TableEntity
    {
        public UpdateDomainEntity()
        {
        }

        public UpdateDomainEntity(string partitionKey, string rowKey, string updateDomain)
            : base(partitionKey, rowKey)
        {
            UpdateDomain = updateDomain;
        }

        public string UpdateDomain
        {
            get; set;
        }
    }
}