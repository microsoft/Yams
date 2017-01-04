using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Etg.Yams.Process
{
    /// <summary>
    /// A Wrapper around the <see cref="System.Diagnostics.Process"/>.
    /// </summary>
    public class Process : IProcess
    {
        private readonly string _exePath;
        private readonly bool _showProcessWindow;
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

        public Process(string exePath, bool showProcessWindow)
        {
            _exePath = exePath;
            _showProcessWindow = showProcessWindow;
        }

        public async Task Start(string args)
        {
            if (_isRunning)
            {
                throw new Exception("Cannot start a process that is already running");
            }
            ExeArgs = args;
            await Task.Run(async () =>
            {
                _process = new System.Diagnostics.Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        FileName = _exePath,
                        WorkingDirectory = new FileInfo(_exePath).Directory.FullName,
                        WindowStyle = ProcessWindowStyle.Normal,
                        Arguments = ExeArgs
                    },
                    EnableRaisingEvents = true,
                };
                _process.Exited += ProcessExited;

                if (!_showProcessWindow)
                {
                    _process.StartInfo.RedirectStandardOutput = true;
                    _process.StartInfo.UseShellExecute = false;
                    _process.StartInfo.CreateNoWindow = true;
                }

                if (!_process.Start())
                {
                    await ReleaseResources();
                    throw new Exception(
                        $"The OS failed to start the process at {_exePath} with arguments {ExeArgs}");
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
                //TODO: Use Process.WaitForExit to wait for the process to exit
                _isRunning = _process.CloseMainWindow();
            });
        }

        public Task Kill()
        {
            if (_process == null) return Task.FromResult(true);

            _process.Exited -= ProcessExited;
            return Task.Run(() =>
            {
                //TODO: Use Process.WaitForExit to wait for the process to exit
                _process.Kill();
                _isRunning = false;
            });
        }

        public bool HasExited => _process.HasExited;

        protected void ProcessExited(object sender, EventArgs e)
        {
            _isRunning = false;
            var handler = Exited;
            if (handler == null)
            {
                return;
            }
            string msg = $"The process {_exePath} has exited with exit code {_process.ExitCode}";
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

        public string ExePath => _exePath;
        public string ExeArgs { get; private set; }

        public void Dispose()
        {
            Kill().Wait();
            ReleaseResources().Wait();
        }
    }
}
