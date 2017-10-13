namespace Etg.Yams.Storage.Status
{
    public interface IDeploymentStatusSerializer
    {
        InstanceDeploymentStatus Deserialize(string data);
        string Serialize(InstanceDeploymentStatus instanceDeploymentStatus);
    }
}
