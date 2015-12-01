using System;
using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Process;
using Etg.Yams.Test.Utils;
using Etg.Yams.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Etg.Yams.Test.Process
{
    [TestClass]
    public class ProcessTest
    {
        private readonly string _testDirPath;
        private readonly string _suicidalExePath;
        private readonly string _hangingExePath;

        const string SuicidalProcessExeName = "SuicidalProcess.exe";
        const string HangingProcessExeName = "HangingProcess.exe";

        public ProcessTest()
        {
            _testDirPath = Path.Combine(Directory.GetCurrentDirectory(), "ProcessTest");
            _suicidalExePath = Path.Combine(_testDirPath, SuicidalProcessExeName);
            _hangingExePath = Path.Combine(_testDirPath, HangingProcessExeName);
        }

        [TestInitialize]
        public void Initialize()
        {
            Directory.CreateDirectory(_testDirPath);

            string[] exes = { SuicidalProcessExeName, HangingProcessExeName };
            foreach (string exeName in exes)
            {
                TestUtils.CopyExe(exeName, _testDirPath);
            }
        }

        [TestMethod]
        public async Task TestIsRunning()
        {
            IProcess hangingProcess = new Yams.Process.Process(_hangingExePath, "");
            Assert.IsFalse(hangingProcess.IsRunning);
            await hangingProcess.Start();
            Assert.IsTrue(hangingProcess.IsRunning);
            await hangingProcess.Kill();
            Assert.IsTrue(await ProcessUtils.SpinWaitForExit(hangingProcess, 5));
            Assert.IsFalse(hangingProcess.IsRunning);
        }

        [TestMethod]
        public async Task TestThatProcessCannotBeStartedMoreThanOnce()
        {
            IProcess hangingProcess = new Yams.Process.Process(_hangingExePath, "");
            await hangingProcess.Start();
            Assert.IsTrue(hangingProcess.IsRunning);
            try
            {
                await hangingProcess.Start();
                Assert.Fail("An exception should have been thrown because of the attempt of starting a process that is already running");
            }
            catch (Exception)
            {
                // ignored
            }
            await hangingProcess.Kill();
            await hangingProcess.ReleaseResources();
        }

        [TestMethod]
        public async Task TestReleaseResources()
        {
            IProcess hangingProcess = new Yams.Process.Process(_hangingExePath, "");
            await hangingProcess.ReleaseResources(); // should do nothing
            await hangingProcess.Start();
            Assert.IsTrue(hangingProcess.IsRunning);

            try
            {
                await hangingProcess.ReleaseResources();
                Assert.Fail("An exception should have been thrown because ReleaseResources was called before stopping the process");
            }
            catch (Exception)
            {
                // ignored
            }

            await hangingProcess.Kill();
            await hangingProcess.ReleaseResources();
        }

        [TestMethod]
        public async Task TestThatExitedEventIsFired()
        {
            IProcess suicidalProcess = new Yams.Process.Process(_suicidalExePath, "");
            bool exitedFired = false;
            suicidalProcess.Exited += (sender, args) =>
            {
                exitedFired = true;
            };
            await suicidalProcess.Start();
            Assert.IsTrue(await ProcessUtils.SpinWaitForExit(suicidalProcess, 5));
            Assert.IsTrue(exitedFired);
            await suicidalProcess.ReleaseResources();
        }

        [TestMethod]
        public void TestProperties()
        {
            const string exeArgs = "arggg";
            IProcess suicidalProcess = new Yams.Process.Process(_suicidalExePath, exeArgs);
            Assert.AreEqual(_suicidalExePath, suicidalProcess.ExePath);
            Assert.AreEqual(exeArgs, suicidalProcess.ExeArgs);
        }
    }
}
