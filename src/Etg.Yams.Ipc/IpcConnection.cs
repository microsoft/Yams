using System;
using System.IO;
using System.Threading.Tasks;

namespace Etg.Yams.Ipc
{
    public class IpcConnection : IIpcConnection
    {
        private readonly INamedPipe _namedPipe;
        private StreamWriter _streamWriter;
        private StreamReader _streamReader;

        public IpcConnection(INamedPipe namedPipe)
        {
            _namedPipe = namedPipe;
        }

        public async Task SendMessage(string message)
        {
            EnsureConnected();
            await _streamWriter.WriteLineAsync(message);
        }

        public Task<string> ReadMessage()
        {
            EnsureConnected();
            return _streamReader.ReadLineAsync();
        }

        public async Task Connect()
        {
            if(_namedPipe.IsConnected)
            {
                throw new InvalidOperationException($"Named pipe {ConnectionId} is already connected");
            }
            await Task.Run(() => _namedPipe.Connect());
            _streamWriter = new StreamWriter(_namedPipe.Stream) {AutoFlush = true};
            _streamReader = new StreamReader(_namedPipe.Stream);
        }

        public string ConnectionId => _namedPipe.PipeName;

        public void Dispose()
        {
            Disconnect().Wait();
            _namedPipe.Dispose();
        }

        public Task Disconnect()
        {
            _streamWriter?.Dispose();
            _streamWriter = null;
            _streamReader?.Dispose();
            _streamReader = null;
            if (_namedPipe.IsConnected)
            {
                _namedPipe.Disconnect();
            }
            return Task.FromResult(true);
        }

        private void EnsureConnected()
        {
            if (!_namedPipe.IsConnected)
            {
                throw new InvalidOperationException("You must connect first");
            }
        }
    }
}