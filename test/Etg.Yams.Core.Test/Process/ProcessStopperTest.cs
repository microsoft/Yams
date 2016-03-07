using System;
using System.Threading.Tasks;
using Etg.Yams.Process;
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
                HasExited_Get = () => true,
                IProcessInfo_ExePath_Get= () => "exePath",
                IProcessInfo_ExeArgs_Get = () => "exeArgs",
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
                HasExited_Get = () => hasExited,
                IProcessInfo_ExePath_Get = () => "exePath",
                IProcessInfo_ExeArgs_Get = () => "exeArgs",
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
                HasExited_Get = () => hasExited,
                IProcessInfo_ExePath_Get = () => "exePath",
                IProcessInfo_ExeArgs_Get = () => "exeArgs",
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
                HasExited_Get = () => false,
                IProcessInfo_ExePath_Get = () => "exePath",
                IProcessInfo_ExeArgs_Get = () => "exeArgs",
            };

            IProcessStopper processStopper = new ProcessStopper(0);
            processStopper.StopProcess(process);
        }
    }
}
