using System;
using System.Threading.Tasks;

namespace Etg.Yams.Ipc
{
    public interface IIpcConnection : IDisposable
    {
        Task SendMessage(string message);
        Task<string> ReadMessage();
        Task Connect();
        Task Disconnect();
        string ConnectionId { get; }
    }
}