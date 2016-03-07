using System;
using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Process;
using Etg.Yams.Test.stubs;
using Xunit;

namespace Etg.Yams.Test.Application
{
    public class ApplicationTestFixture : IDisposable
    {
        public const string ExeName = "exeName";
        public ApplicationConfig AppConfig { get; set; }
        private AppIdentity _appIdentity;
        public const string AppPath = "path";
        public const string TestExeArgs = "arg1, arg2";

        public ApplicationTestFixture()
        {
            _appIdentity = new AppIdentity("id", new Version("1.0.0"));
            AppConfig = new ApplicationConfig(_appIdentity, ExeName, TestExeArgs);
        }

        public void Dispose() { }
    }

    public class ApplicationTest : IClassFixture<ApplicationTestFixture>
    {
        private ApplicationConfig _appConfig;

        public ApplicationTest(ApplicationTestFixture fixture)
        {
            _appConfig = fixture.AppConfig;
        }

        [Fact]
        public async Task TestStartProcessHappyPath()
        {
            int startCallCount = 0;
            int stopCallCount = 0;
            IProcess process = new StubIProcess()
            {
                Start = () =>
                {
                    startCallCount++;
                    return Task.FromResult(true);
                }
            };

            string exePath = "";
            string exeArgs = "";

            IProcessFactory processFactory = new StubIProcessFactory()
            {
                
                CreateProcess_String_String = (path, args) =>
                {
                    exePath = path;
                    exeArgs = args;
                    return process;
                }
            };

            IProcessStopper processStopper = new StubIProcessStopper
            {
                StopProcess_IProcess = (p) =>
                {
                    ++stopCallCount;
                    return Task.FromResult(true);
                }
            };

            ConfigurableApplication application = new ConfigurableApplication(ApplicationTestFixture.AppPath, _appConfig, processFactory, processStopper);
            await application.Start();

            Assert.Equal(1, startCallCount);
            Assert.Equal(0, stopCallCount);
            Assert.Equal(ApplicationTestFixture.TestExeArgs, exeArgs);
            Assert.Equal(Path.Combine(ApplicationTestFixture.AppPath, ApplicationTestFixture.ExeName), exePath);
        }

        [Fact]
        public async Task TestThatStartFailsIfProcessFailsToStart()
        {
            IProcess process = new StubIProcess()
            {
                Start = () =>
                {
                    throw new Exception("Process failed to start");
                }
            };

            IProcessFactory processFactory = new StubIProcessFactory()
            {
                CreateProcess_String_String = (path, args) => process
            };

            IProcessStopper processStopper = new StubIProcessStopper();

            ConfigurableApplication application = new ConfigurableApplication(ApplicationTestFixture.AppPath, _appConfig, processFactory, processStopper);
            Assert.False(await application.Start());
        }

        [Fact]
        public async Task TestThatStopStopsTheProcess()
        {
            int stopCallCount = 0;
            IProcess process = new StubIProcess()
            {
                Start = () => Task.FromResult(true)
            };

            IProcessStopper processStopper = new StubIProcessStopper
            {
                StopProcess_IProcess = (p) =>
                {
                    ++stopCallCount;
                    return Task.FromResult(true);
                }
            };

            IProcessFactory processFactory = new StubIProcessFactory()
            {
                CreateProcess_String_String = (path, args) => process
            };
            ConfigurableApplication application = new ConfigurableApplication(ApplicationTestFixture.AppPath, _appConfig, processFactory, processStopper);
            await application.Start();
            await application.Stop();

            Assert.Equal(1, stopCallCount);
        }

        [Fact]
        public async Task TestThatExitedEventIsEmittedWhenProcessFails()
        {
            ProcessStub process = new ProcessStub("", "");
            IProcessFactory processFactory = new StubIProcessFactory()
            {
                CreateProcess_String_String = (path, args) => process
            };

            ConfigurableApplication application = new ConfigurableApplication(ApplicationTestFixture.AppPath, _appConfig, processFactory, new StubIProcessStopper());
            ApplicationExitedArgs appExitedArgs = null;
            int exitedEventCount = 0;
            application.Exited += (sender, args) =>
            {
                appExitedArgs = args;
                ++exitedEventCount;
            };
            await application.Start();
            process.RaiseExitedEvent();

            Assert.Equal(1, exitedEventCount);
            Assert.Equal(_appConfig.Identity, appExitedArgs.AppIdentity);
        }

    }
}
