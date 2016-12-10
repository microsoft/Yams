namespace Etg.Yams.Client
{
    public interface IYamsClientFactory
    {
        IYamsClient CreateYamsClient(YamsClientConfig config);
    }
}