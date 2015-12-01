using System;
using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Process;
using Etg.Yams.Process.Fakes;
using Etg.Yams.Test.stubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Etg.Yams.Test.Application
{
    [TestClass]
    public class ApplicationTest
    {
        private const string ExeName = "exeName";
        private static ApplicationConfig _appConfig;
        private static AppIdentity _appIdentity;
        private const string AppPath = "path";
        private const string TestExeArgs = "arg1, arg2";

        [ClassInitialize]
        public static void ClassInitialize(TestContext cont)
        {
            _appIdentity = new AppIdentity("id", new Version("1.0.0"));
            _appConfig = new ApplicationConfig(_appIdentity, ExeName, TestExeArgs);
        }

        [TestMethod]
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
                CreateProcessStringString = (path, args) =>
                {
                    exePath = path;
                    exeArgs = args;
                    return process;
                }
            };

            IProcessStopper processStopper = new StubIProcessStopper
            {
                StopProcessIProcess = (p) =>
                {
                    ++stopCallCount;
                    return Task.FromResult(true);
                }
            };

            ConfigurableApplication application = new ConfigurableApplication(AppPath, _appConfig, processFactory, processStopper);
            await application.Start();

            Assert.AreEqual(1, startCallCount);
            Assert.AreEqual(0, stopCallCount);
            Assert.AreEqual(TestExeArgs, exeArgs);
            Assert.AreEqual(Path.Combine(AppPath, ExeName), exePath);
        }

        [TestMethod]
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
                CreateProcessStringString = (path, args) => process
            };

            IProcessStopper processStopper = new StubIProcessStopper();

            ConfigurableApplication application = new ConfigurableApplication(AppPath, _appConfig, processFactory, processStopper);
            Assert.IsFalse(await application.Start());
        }

        [TestMethod]
        public async Task TestThatStopStopsTheProcess()
        {
            int stopCallCount = 0;
            IProcess process = new StubIProcess()
            {
                Start = () => Task.FromResult(true)
            };

            IProcessStopper processStopper = new StubIProcessStopper
            {
                StopProcessIProcess = (p) =>
                {
                    ++stopCallCount;
                    return Task.FromResult(true);
                }
            };

            IProcessFactory processFactory = new StubIProcessFactory()
            {
                CreateProcessStringString = (path, args) => process
            };
            ConfigurableApplication application = new ConfigurableApplication(AppPath, _appConfig, processFactory, processStopper);
            await application.Start();
            await application.Stop();

            Assert.AreEqual(1, stopCallCount);
        }

        [TestMethod]
        public async Task TestThatExitedEventIsEmittedWhenProcessFails()
        {
            ProcessStub process = new ProcessStub("", "");
            IProcessFactory processFactory = new StubIProcessFactory()
            {
                CreateProcessStringString = (path, args) => process
            };

            ConfigurableApplication application = new ConfigurableApplication(AppPath, _appConfig, processFactory, new StubIProcessStopper());
            ApplicationExitedArgs appExitedArgs = null;
            int exitedEventCount = 0;
            application.Exited += (sender, args) =>
            {
                appExitedArgs = args;
                ++exitedEventCount;
            };
            await application.Start();
            process.RaiseExitedEvent();

            Assert.AreEqual(1, exitedEventCount);
            Assert.AreEqual(_appIdentity, appExitedArgs.AppIdentity);
        }

    }
}
