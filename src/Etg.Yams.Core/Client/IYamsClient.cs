using System;
using System.Threading.Tasks;

namespace Etg.Yams.Client
{
    public interface IYamsClient : IDisposable
    {
        Task Connect();
        Task SendHeartBeat();
        Task SendInitializationDoneMessage();
        event EventHandler ExitMessageReceived;
    }
}