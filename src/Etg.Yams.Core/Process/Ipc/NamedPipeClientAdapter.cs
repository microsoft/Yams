using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;

namespace Etg.Yams.Process.Ipc
{
    // This class is excluded from code coverage because it's being covered by E2E tests but the code coverage
    // analyzer cannot see it because it's being run in a separate test process.
    [ExcludeFromCodeCoverage]
    public class NamedPipeClientAdapter : INamedPipe
    {
        private readonly NamedPipeClientStream _client;

        public Stream Stream => _client;
        public bool IsConnected => _client.IsConnected;
        public string PipeName { get; }

        public NamedPipeClientAdapter(string pipeName)
        {
            _client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            PipeName = pipeName;
        }

        public void Connect()
        {
            _client.Connect();
        }

        public void Disconnect()
        {
            if (_client.IsConnected)
            {
                _client.Close();
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}