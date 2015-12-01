using System;
using System.Threading.Tasks;
using Etg.Yams.Process;
using Etg.Yams.Process.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Etg.Yams.Test.Process
{
    [TestClass]
    public class ProcessStopperTest
    {
        [TestMethod]
        public void TestThatProcessIsStoppedGracefullyFirst()
        {
            bool resourcesReleased = false;
            IProcess process = new StubIProcess
            {
                Close = () =>
                {
                    throw new Exception("Close should not be called if the process has exited gracefully");
                },
                Kill = () =>
                {
                    throw new Exception("Kill should not be called if the process has exited gracefully");
                },
                HasExitedGet = () => true,
                ExePathGet = () => "exePath",
                ExeArgsGet = () => "exeArgs",
                ReleaseResources = () =>
                {
                    resourcesReleased = true;
                    return Task.FromResult(true);
                }
            };

            IProcessStopper processStopper = new ProcessStopper(0);
            processStopper.StopProcess(process);

            Assert.IsTrue(resourcesReleased);
        }

        [TestMethod]
        public void TestThatProcessIsClosedIfItWontExitGracefullyExit()
        {
            bool resourcesReleased = false;
            bool hasExited = false;
            IProcess process = new StubIProcess
            {
                Close = () =>
                {
                    hasExited = true;
                    return Task.FromResult(true);
                },
                Kill = () =>
                {
                    throw new Exception("Kill should not be called if the process has exited gracefully");
                },
                HasExitedGet = () => hasExited,
                ExePathGet = () => "exePath",
                ExeArgsGet = () => "exeArgs",
                ReleaseResources = () =>
                {
                    resourcesReleased = true;
                    return Task.FromResult(true);
                }
            };

            IProcessStopper processStopper = new ProcessStopper(0);
            processStopper.StopProcess(process);

            Assert.IsTrue(hasExited);
            Assert.IsTrue(resourcesReleased);
        }

        [TestMethod] 
        public void TestThatProcessIsKilledIfItWontExitGracefullyExitNorClose()
        {
            bool resourcesReleased = false;
            bool hasExited = false;
            IProcess process = new StubIProcess
            {
                Close = () => Task.FromResult(true),
                Kill = () =>
                {
                    hasExited = true;
                    return Task.FromResult(true);
                },
                HasExitedGet = () => hasExited,
                ExePathGet = () => "exePath",
                ExeArgsGet = () => "exeArgs",
                ReleaseResources = () =>
                {
                    resourcesReleased = true;
                    return Task.FromResult(true);
                }
            };

            IProcessStopper processStopper = new ProcessStopper(0);
            processStopper.StopProcess(process);

            Assert.IsTrue(hasExited);
            Assert.IsTrue(resourcesReleased);
        }

        [TestMethod]
        public void TestThatExceptionIsCaughtAndSwallowedIfKillBlowsUp()
        {
            IProcess process = new StubIProcess
            {
                Close = () => Task.FromResult(true),
                Kill = () =>
                {
                    throw new Exception("Process would not die!");
                },
                HasExitedGet = () => false,
                ExePathGet = () => "exePath",
                ExeArgsGet = () => "exeArgs",
            };

            IProcessStopper processStopper = new ProcessStopper(0);
            processStopper.StopProcess(process);
        }
    }
}
