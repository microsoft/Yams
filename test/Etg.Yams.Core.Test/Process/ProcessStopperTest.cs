using System;
using System.Threading.Tasks;
using Etg.Yams.Process;
using Etg.Yams.Process.Fakes;
using Xunit;

namespace Etg.Yams.Test.Process
{
    public class ProcessStopperTest
    {
        [Fact]
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

            Assert.True(resourcesReleased);
        }

        [Fact]
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

            Assert.True(hasExited);
            Assert.True(resourcesReleased);
        }

        [Fact] 
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

            Assert.True(hasExited);
            Assert.True(resourcesReleased);
        }

        [Fact]
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
