namespace Etg.Yams.Ipc
{
    public class NamedPipeFactory : INamedPipeFactory
    {
        public INamedPipe CreateServer(string pipeName)
        {
            return new NamedPipeServerAdapter(pipeName);   
        }

        public INamedPipe CreateClient(string pipeName)
        {
            return new NamedPipeClientAdapter(pipeName);
        }
    }
}