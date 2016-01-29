using System.Diagnostics;
using System.Threading.Tasks;
using Etg.Yams.Process;

namespace Etg.Yams.Utils
{
    public static class ProcessUtils
    {
        public static async Task<bool> SpinWaitForExit(IProcess process, int maxTimeInSeconds)
        {
            int waitForExitInSecondsRemaining = maxTimeInSeconds;
            Trace.TraceInformation("Waiting for the process to terminate");
            while (!process.HasExited && waitForExitInSecondsRemaining > 0)
            {
                await Task.Delay(1000);
                waitForExitInSecondsRemaining--;
            }
            return process.HasExited;
        }
    }
}