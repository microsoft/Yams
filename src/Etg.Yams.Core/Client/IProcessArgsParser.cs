namespace Etg.Yams.Client
{
    public interface IProcessArgsParser
    {
        YamsClientOptions ParseArgs(string[] args);
    }
}