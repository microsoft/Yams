using System;
using System.Threading.Tasks;
using Etg.Yams.Process;
using Etg.Yams.Test.stubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Etg.Yams.Test.Process
{

    [TestClass]
    public class SelfRestartingProcessTest
    {
        [TestMethod]
        public async Task TestThatProcessIsRestarted()
        {
            ProcessStub process = new ProcessStub("exePath", "exeArgs");

            SelfRestartingProcess selfRestartingProcess = new SelfRestartingProcess(process, 1);
            await selfRestartingProcess.Start();
            Assert.IsTrue(process.IsRunning);

            process.RaiseExitedEvent();
            Assert.IsTrue(await SpinWaitForRestart(selfRestartingProcess, 1));
            Assert.IsTrue(process.IsRunning);
        }

        [TestMethod]
        public async Task TestThatExitedIsRaisedIfProcessFailsToRestart()
        {
            ProcessStub process = new ProcessStub("exePath", "exeArgs");
            SelfRestartingProcess selfRestartingProcess = new SelfRestartingProcess(process, 1);
            bool exitedFired = false;
            selfRestartingProcess.Exited += (sender, args) => {
                exitedFired = true;
            };

            await selfRestartingProcess.Start();
            Assert.IsTrue(process.IsRunning);

            process.ShouldStart = false;
            process.RaiseExitedEvent();

            Assert.IsTrue(await SpinWaitFor(() => exitedFired));
            Assert.IsFalse(process.IsRunning);
            Assert.IsTrue(exitedFired);
        }

        [TestMethod]
        public async Task TestThatExitedIsRaisedIfProcessMaxRetryCountIsReached()
        {
            ProcessStub process = new ProcessStub("exePath", "exeArgs");
            SelfRestartingProcess selfRestartingProcess = new SelfRestartingProcess(process, 1);
            bool exitedFired = false;
            selfRestartingProcess.Exited += (sender, args) =>
            {
                exitedFired = true;
            };

            await selfRestartingProcess.Start();
            Assert.IsTrue(process.IsRunning);

            process.RaiseExitedEvent();
            Assert.IsTrue(await SpinWaitForRestart(selfRestartingProcess, 1));
            Assert.IsTrue(process.IsRunning);

            process.RaiseExitedEvent();
            Assert.IsTrue(await SpinWaitFor(() => exitedFired));
            Assert.IsFalse(process.IsRunning);
            Assert.IsTrue(exitedFired);
            
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
