using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Etg.Yams.Process
{
    /// <summary>
    /// A Wrapper around the <see cref="System.Diagnostics.Process"/>.
    /// </summary>
    public class Process : IProcess
    {
        private readonly string _exePath;
        private readonly string _exeArgs;
        private System.Diagnostics.Process _process;
        private bool _isRunning = false;

        public bool IsRunning
        {
            get
            {
                if (_process == null)
                    return false;
                if (_process.HasExited)
                    return false;
                return _isRunning;
            }
        }

        public event EventHandler<ProcessExitedArgs> Exited;

        public Process(string exePath, string exeArgs)
        {
            _exePath = exePath;
            _exeArgs = exeArgs;
        }

        public async Task Start()
        {
            if (_isRunning)
            {
                throw new Exception("Cannot start a process that is already running");
            }
            await Task.Run(async () =>
            {
                _process = new System.Diagnostics.Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        UseShellExecute = false,
                        FileName = _exePath,
                        WorkingDirectory = new FileInfo(_exePath).Directory.FullName,
                        WindowStyle = ProcessWindowStyle.Normal,
                        Arguments = _exeArgs
                    },
                    EnableRaisingEvents = true,
                };
                _process.Exited += ProcessExited;

                if (!_process.Start())
                {
                    await ReleaseResources();
                    throw new Exception(string.Format("The OS failed to start the process at {0} with arguments {1}", _exePath, _exeArgs));
                }
                _isRunning = true;
            });
        }

        public async Task Close()
        {
            if (_process == null) return;

            _process.Exited -= ProcessExited;
            await Task.Run(() =>
            {
                _isRunning = _process.CloseMainWindow();
            });
        }

        public Task Kill()
        {
            if (_process == null) return Task.FromResult(true);

            _process.Exited -= ProcessExited;
            return Task.Run(() =>
            {
                _process.Kill();
                _isRunning = false;
            });
        }

        public bool HasExited
        {
            get { return _process.HasExited; }
        }

        protected void ProcessExited(object sender, EventArgs e)
        {
            _isRunning = false;
            var handler = Exited;
            if (handler == null)
            {
                return;
            }
            string msg = string.Format("The process {0} has exited with exit code {1}", _exePath, _process.ExitCode);
            Trace.TraceInformation(msg);
            handler(this, new ProcessExitedArgs(this, msg));
        }

        public async Task ReleaseResources()
        {
            if (IsRunning)
            {
                throw new Exception("You should stop the process before releasing resources");
            }
            _isRunning = false;
            if (_process != null)
            {
                await Task.Run(() =>
                {
                    _process.Exited -= ProcessExited;
                    _process.Dispose();
                    _process = null;
                });
            }
        }

        public string ExePath
        {
            get { return _exePath; }
        }

        public string ExeArgs
        {
            get { return _exeArgs; }
        }

        public void Dispose()
        {
            Kill().Wait();
            ReleaseResources().Wait();
        }
    }
}
