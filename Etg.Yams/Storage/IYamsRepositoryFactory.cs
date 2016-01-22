namespace Etg.Yams.Storage
{
    public interface IYamsRepositoryFactory
    {
        IYamsRepository CreateRepository(string connectionString);
    }
}