using System;
using System.Diagnostics;
using System.Threading;
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
                if (!await Close(process))
                {
                    Trace.TraceInformation("The host process would not close, attempting to kill");
                    await process.Kill();
                    Trace.TraceInformation("The host process was killed");
                }
                else
                {
                    Trace.TraceInformation("Process Closed");
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

        private async Task<bool> Close(IProcess process)
        {
            await process.Close();
            return await ProcessUtils.SpinWaitForExit(process, _waitForExitInSeconds);
        }
    }
}