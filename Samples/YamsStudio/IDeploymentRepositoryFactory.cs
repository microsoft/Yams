using Etg.Yams.Storage;

namespace YamsStudio
{
    public interface IDeploymentRepositoryFactory
    {
        IDeploymentRepository CreateRepository(string connectionString);
    }
}