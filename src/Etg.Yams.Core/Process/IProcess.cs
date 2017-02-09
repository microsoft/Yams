using System;
using System.Threading.Tasks;

namespace Etg.Yams.Process
{
    public interface IProcess : IProcessInfo, IDisposable
    {
        Task Start();

        void StopGracefully();

        Task Close();

        Task Kill();

        Task ReleaseResources();

        bool HasExited { get; }

        bool IsRunning { get; }

        event EventHandler<ProcessExitedArgs> Exited;
    }

    public class ProcessExitedArgs : EventArgs
    {
        public ProcessExitedArgs(IProcessInfo processInfo, string message)
        {
            ProcessInfo = processInfo;
            Message = message;
        }

        public string Message { get; private set; }

        public IProcessInfo ProcessInfo { get; private set; }
    }
}