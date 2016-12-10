using System;
using System.Threading.Tasks;
using Etg.Yams.Process;
using Etg.Yams.Test.stubs;
using Xunit;

namespace Etg.Yams.Test.Process
{
    public class SelfRestartingProcessTest
    {
        [Fact]
        public async Task TestThatProcessIsRestarted()
        {
            ProcessStub process = new ProcessStub("exePath");

            SelfRestartingProcess selfRestartingProcess = new SelfRestartingProcess(process, 1);
            await selfRestartingProcess.Start("exeArgs");
            Assert.True(process.IsRunning);

            process.RaiseExitedEvent();
            Assert.True(await SpinWaitForRestart(selfRestartingProcess, 1));
            Assert.True(process.IsRunning);
        }

        [Fact]
        public async Task TestThatExitedIsRaisedIfProcessFailsToRestart()
        {
            ProcessStub process = new ProcessStub("exePath");
            SelfRestartingProcess selfRestartingProcess = new SelfRestartingProcess(process, 1);
            bool exitedFired = false;
            selfRestartingProcess.Exited += (sender, args) => {
                exitedFired = true;
            };

            await selfRestartingProcess.Start("exeArgs");
            Assert.True(process.IsRunning);

            process.ShouldStart = false;
            process.RaiseExitedEvent();

            Assert.True(await SpinWaitFor(() => exitedFired));
            Assert.False(process.IsRunning);
            Assert.True(exitedFired);
        }

        [Fact]
        public async Task TestThatExitedIsRaisedIfProcessMaxRetryCountIsReached()
        {
            ProcessStub process = new ProcessStub("exePath");
            SelfRestartingProcess selfRestartingProcess = new SelfRestartingProcess(process, 1);
            bool exitedFired = false;
            selfRestartingProcess.Exited += (sender, args) =>
            {
                exitedFired = true;
            };

            await selfRestartingProcess.Start("exeArgs");
            Assert.True(process.IsRunning);

            process.RaiseExitedEvent();
            Assert.True(await SpinWaitForRestart(selfRestartingProcess, 1));
            Assert.True(process.IsRunning);

            process.RaiseExitedEvent();
            Assert.True(await SpinWaitFor(() => exitedFired));
            Assert.False(process.IsRunning);
            Assert.True(exitedFired);
            
        }

        private Task<bool> SpinWaitForRestart(SelfRestartingProcess selfRestartingProcess, int restartCount)
        {
            return SpinWaitFor(() => selfRestartingProcess.RestartCount == restartCount);
        }

        private async Task<bool> SpinWaitFor(Func<bool> func)
        {
            int totalWaitTime = 1000;
            const int waitIncrement = 100;
            while (!func() && totalWaitTime > 0)
            {
                await Task.Delay(waitIncrement);
                totalWaitTime -= waitIncrement;
            }
            return func();
        }

        
    }
}
