namespace Etg.Yams.Update
{
    public class UpdateSessionManagerConfig
    {
        public UpdateSessionManagerConfig(string cloudServiceDeploymentId, string instanceUpdateDomain, string instanceId, string storageContainerName)
        {
            CloudServiceDeploymentId = cloudServiceDeploymentId;
            InstanceUpdateDomain = instanceUpdateDomain;
            InstanceId = instanceId;
            StorageContainerName = storageContainerName;
        }

        public string CloudServiceDeploymentId { get; private set; }
        public string InstanceUpdateDomain { get; private set; }
        public string InstanceId { get; private set; }
        public string StorageContainerName { get; private set; }
    }
}