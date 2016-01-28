using System;
using System.Threading.Tasks;
using Etg.Yams.Process;

namespace Etg.Yams.Test.stubs
{
    public class ProcessStub : IProcess
    {
        private readonly string _exePath;
        private readonly string _exeArgs;

        public ProcessStub(string exePath, string exeArgs)
        {
            _exePath = exePath;
            _exeArgs = exeArgs;
            ShouldStart = true;
        }

        public string ExePath
        {
            get { return _exePath; }
        }

        public string ExeArgs
        {
            get { return _exeArgs; }
        }

        public Task Start()
        {
            if (!ShouldStart)
            {
                throw new Exception("Cannot start process");
            }
            IsRunning = true;
            return Task.FromResult(true);
        }

        public Task Close()
        {
            IsRunning = false;
            return Task.FromResult(true);
        }

        public Task Kill()
        {
            IsRunning = false;
            return Task.FromResult(true);
        }

        public Task ReleaseResources()
        {
            IsRunning = false;
            return Task.FromResult(true);
        }

        public bool HasExited
        {
            get { return !IsRunning; }
        }

        public bool IsRunning
        {
            get;
            private set;
        }

        public bool ShouldStart { get; set; }

        public void RaiseExitedEvent()
        {
            var failedEvent = Exited;
            if (failedEvent == null) return;
            failedEvent(this, new ProcessExitedArgs(this, "process Exited event raised from stub"));
        }

        public event EventHandler<ProcessExitedArgs> Exited;
        public void Dispose()
        {
        }
    }
}
