using System;
using System.Threading.Tasks;

namespace Etg.Yams.Process
{
    public interface IProcess : IDisposable
    {
        Task Start(string args);

        Task Close();

        Task Kill();

        Task ReleaseResources();

        bool HasExited { get; }

        bool IsRunning { get; }

        event EventHandler<ProcessExitedArgs> Exited;

        string ExePath { get; }
    }

    public class ProcessExitedArgs : EventArgs
    {
        public ProcessExitedArgs(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }
}