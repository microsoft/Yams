namespace Etg.Yams.Storage.Config
{
    public interface IDeploymentConfigSerializer
    {
        DeploymentConfig Deserialize(string data);
        string Serialize(DeploymentConfig deploymentConfig);
    }
}