using System;
using System.Threading.Tasks;
using Etg.Yams.Process;
using Xunit;

namespace Etg.Yams.Test.Process
{
    public class ProcessStopperTest
    {
        [Fact]
        public void TestThatProcessIsClosedFirst()
        {
            bool resourcesReleased = false;
            bool hasExited = false;
            IProcess process = new StubIProcess()
                .Close(() =>
                {
                    hasExited = true;
                    return Task.FromResult(true);
                })
                .Kill(() =>
                {
                    throw new Exception("Kill should not be called if the process has exited gracefully");
                })
                .HasExited_Get(() => hasExited)
                .ExePath_Get(() => "exePath")
                .ExeArgs_Get(() => "exeArgs")
                .ReleaseResources(() =>
                {
                    resourcesReleased = true;
                    return Task.FromResult(true);
                }
            );

            IProcessStopper processStopper = new ProcessStopper(0);
            processStopper.StopProcess(process);

            Assert.True(hasExited);
            Assert.True(resourcesReleased);
        }

        [Fact] 
        public void TestThatProcessIsKilledIfItWontClose()
        {
            bool resourcesReleased = false;
            bool hasExited = false;
            IProcess process = new StubIProcess()
                .Close(() => Task.FromResult(true))
                .Kill(() =>
                {
                    hasExited = true;
                    return Task.FromResult(true);
                })
                .HasExited_Get(() => hasExited)
                .ExePath_Get(() => "exePath")
                .ExeArgs_Get(() => "exeArgs")
                .ReleaseResources(() =>
                {
                    resourcesReleased = true;
                    return Task.FromResult(true);
                }
            );

            IProcessStopper processStopper = new ProcessStopper(0);
            processStopper.StopProcess(process);

            Assert.True(hasExited);
            Assert.True(resourcesReleased);
        }

        [Fact]
        public void TestThatExceptionIsCaughtAndSwallowedIfKillBlowsUp()
        {
            IProcess process = new StubIProcess()
                .Close(() => Task.FromResult(true))
                .Kill(() =>
                {
                    throw new Exception("Process would not die!");
                })
                .HasExited_Get(() => false)
                .ExePath_Get(() => "exePath")
                .ExeArgs_Get(() => "exeArgs");

            IProcessStopper processStopper = new ProcessStopper(0);
            processStopper.StopProcess(process);
        }
    }
}
