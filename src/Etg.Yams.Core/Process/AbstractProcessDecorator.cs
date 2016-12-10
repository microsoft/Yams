using System;
using System.Threading.Tasks;

namespace Etg.Yams.Process
{
    public class AbstractProcessDecorator : IProcess
    {
        protected readonly IProcess _process;

        public event EventHandler<ProcessExitedArgs> Exited;

        public AbstractProcessDecorator(IProcess process)
        {
            _process = process;
            _process.Exited += Exited;
        }

        public string ExePath => _process.ExePath;
        public string ExeArgs => _process.ExeArgs;
        public bool HasExited => _process.HasExited;
        public bool IsRunning => _process.IsRunning;

        public virtual void Dispose()
        {
            _process.Dispose();
        }

        public virtual Task Start(string args)
        {
            return _process.Start(args);
        }

        public virtual Task Close()
        {
            _process.Exited -= Exited;
            return _process.Close();
        }

        public virtual Task Kill()
        {
            _process.Exited -= Exited;
            return _process.Kill();
        }

        public virtual Task ReleaseResources()
        {
            _process.Exited -= Exited;
            return _process.ReleaseResources();
        }

        protected void InvokeExited(ProcessExitedArgs args)
        {
            Exited?.Invoke(this, args);
        }
    }
}