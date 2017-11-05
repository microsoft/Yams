using System.Collections.Generic;

namespace Etg.Yams.Azure.UpdateSession
{
    public class UpdateSessionStatus
    {
        public IEnumerable<UpdateDomainEntity> InstancesEntities { get; }
        public UpdateDomainEntity ModifiedEntity { get; }
        public UpdateDomainEntity UpdateDomainEntity { get; }

        public UpdateSessionStatus(IEnumerable<UpdateDomainEntity> instancesEntities, UpdateDomainEntity modifiedEntity,
            UpdateDomainEntity updateDomainEntity)
        {
            InstancesEntities = instancesEntities;
            ModifiedEntity = modifiedEntity;
            UpdateDomainEntity = updateDomainEntity;
        }
    }
}