using System.IO;
using System.IO.Pipes;

namespace Etg.Yams.Ipc
{
    public class NamedPipeServerAdapter : INamedPipe
    {
        private readonly NamedPipeServerStream _server;

        public NamedPipeServerAdapter(string pipeName)
        {
            _server = new NamedPipeServerStream(pipeName, PipeDirection.InOut);
            PipeName = pipeName;
        }

        public Stream Stream => _server;
        public bool IsConnected => _server.IsConnected;
        public string PipeName { get; }

        public void Connect()
        {
            _server.WaitForConnection();
        }

        public void Disconnect()
        {
            _server.Close();
        }

        public void Dispose()
        {
            _server.Dispose();
        }
    }
}