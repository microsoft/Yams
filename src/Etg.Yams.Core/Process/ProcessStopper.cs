using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Etg.Yams.Utils;

namespace Etg.Yams.Process
{
    public class ProcessStopper : IProcessStopper
    {
        private const int ReleaseResourcesDelayMilliseconds = 5000;

        private readonly int _waitForExitInSeconds;

        public ProcessStopper(int waitForExitInSeconds)
        {
            _waitForExitInSeconds = waitForExitInSeconds;
        }

        public async Task StopProcess(IProcess process)
        {
            string exePath = process.ExePath;
            try
            {
                if (!await StopGracefully(process))
                {
                    Trace.TraceInformation(
                        "Process has not exited gracefully or is not listening to the stop event, let's try to close the main window");
                    if (!await Close(process))
                    {
                        Trace.TraceInformation("The host process would not close, attempting to kill");
                        await process.Kill();
                        Trace.TraceInformation("The host process was killed");
                    }
                    else
                    {
                        Trace.TraceInformation("Process exited with CloseMainWindow");
                    }
                }
                else
                {
                    Trace.TraceInformation("Process exited gracefully");
                }
                await process.ReleaseResources();
                await Task.Delay(ReleaseResourcesDelayMilliseconds); // allowing the os some time to release resources
            }
            catch (Exception ex)
            {
                // there is nothing more we can do; we'll swallow the exception and log an error
                Trace.TraceError("{0} host process could not be killed. Exception was: {1}", exePath, ex);
            }
        }

        private Task<bool> StopGracefully(IProcess process)
        {
            process.StopGracefully();

            return ProcessUtils.SpinWaitForExit(process, _waitForExitInSeconds);
        }

        private async Task<bool> Close(IProcess process)
        {
            await process.Close();
            return await ProcessUtils.SpinWaitForExit(process, _waitForExitInSeconds);
        }
    }
}