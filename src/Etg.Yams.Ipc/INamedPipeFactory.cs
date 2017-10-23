namespace Etg.Yams.Ipc
{
    public interface INamedPipeFactory
    {
        INamedPipe CreateServer(string pipeName);
        INamedPipe CreateClient(string pipeName);
    }
}