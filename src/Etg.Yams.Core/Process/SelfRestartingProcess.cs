using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Etg.Yams.Process
{
    /// <summary>
    /// A process that automatically restarts itself if it exists unexpectedly (for a finite number of trials).
    /// If <see cref="Close"/> or <see cref="Kill"/> is called, the process won't attempt to restart itself.
    /// </summary>
    public class SelfRestartingProcess : IProcess
    {
        private bool _stopped = false;
        private int _restartCount=0;
        private readonly IProcess _process;
        private readonly int _maximumRestartAttempts;

        public bool IsRunning
        {
            get { return _process.IsRunning; }
        }

        public event EventHandler<ProcessExitedArgs> Exited;

        public SelfRestartingProcess(IProcess process, int maximumRestartAttempts)
        {
            _process = process;
            _maximumRestartAttempts = maximumRestartAttempts;
        }

        public Task Start(string args)
        {
            _process.Exited += ExitedTryRestart;
            _stopped = false;
            return _process.Start(args);
        }

        public Task Close()
        {
            _stopped = true;
            return _process.Close();
        }

        public Task Kill()
        {
            _stopped = true;
            return _process.Kill();
        }

        public Task ReleaseResources()
        {
            return _process.ReleaseResources();
        }

        public bool HasExited
        {
            get { return _process.HasExited; }
        }

        public int RestartCount 
        {
            get { return _restartCount;}
        }

        protected async void ExitedTryRestart(object sender, ProcessExitedArgs args)
        {
            // _stopped indicates if the process has been manually stopped; in which case, it should not be restarted.
            if (_stopped) return;
            Trace.TraceInformation("Host process exited, will attempt a restart.");
            await _process.ReleaseResources();

            if (_restartCount >= _maximumRestartAttempts)
            {
                OnFailed("Process {0} has exited {1} times and won't be restarted; failure message {2}", _process.ExePath, _restartCount, args.Message);
            }
            else
            {
                try
                {
                    await _process.Start(ExeArgs);
                    ++_restartCount;
                }
                catch (Exception)
                {
                    OnFailed("Process {0} has exited and could not be restarted; failure message {1}", _process.ExePath, args.Message);
                }
            }
        }

        protected void OnFailed(string format, params object[] args)
        {
            Trace.TraceWarning(format, args);
            var failedEvent = Exited;
            if (failedEvent == null) return;
            failedEvent(this, new ProcessExitedArgs(this, string.Format(format, args)));
        }

        public string ExePath
        {
            get { return _process.ExePath; }
        }

        public string ExeArgs
        {
            get { return _process.ExeArgs; }
        }

        public void Dispose()
        {
            _process.Dispose();
        }
    }
}
