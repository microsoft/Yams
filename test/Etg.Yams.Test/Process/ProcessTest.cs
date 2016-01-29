using System;
using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Process;
using Etg.Yams.Test.Utils;
using Etg.Yams.Utils;
using Xunit;

namespace Etg.Yams.Test.Process
{
    public class ProcessTestFixture
    {
        private readonly string _testDirPath;
        public readonly string SuicidalExePath;
        public readonly string HangingExePath;

        const string SuicidalProcessExeName = "SuicidalProcess.exe";
        const string HangingProcessExeName = "HangingProcess.exe";

        public ProcessTestFixture()
        {
            _testDirPath = Path.Combine(Directory.GetCurrentDirectory(), "ProcessTest");
            SuicidalExePath = Path.Combine(_testDirPath, SuicidalProcessExeName);
            HangingExePath = Path.Combine(_testDirPath, HangingProcessExeName);

            Directory.CreateDirectory(_testDirPath);

            string[] exes = { SuicidalProcessExeName, HangingProcessExeName };
            foreach (string exeName in exes)
            {
                TestUtils.CopyExe(exeName, _testDirPath);
            }
        }
    }

    public class ProcessTest : IClassFixture<ProcessTestFixture>
    {
        private string _hangingExePath;
        private string _suicidalExePath;

        public ProcessTest(ProcessTestFixture fixture)
        {
            _hangingExePath = fixture.HangingExePath;
            _suicidalExePath = fixture.SuicidalExePath;
        }

        [Fact]
        public async Task TestIsRunning()
        {
            IProcess hangingProcess = new Yams.Process.Process(_hangingExePath, "");
            Assert.False(hangingProcess.IsRunning);
            await hangingProcess.Start();
            Assert.True(hangingProcess.IsRunning);
            await hangingProcess.Kill();
            Assert.True(await ProcessUtils.SpinWaitForExit(hangingProcess, 5));
            Assert.False(hangingProcess.IsRunning);
        }

        [Fact]
        public async Task TestThatProcessCannotBeStartedMoreThanOnce()
        {
            IProcess hangingProcess = new Yams.Process.Process(_hangingExePath, "");
            await hangingProcess.Start();
            Assert.True(hangingProcess.IsRunning);

            await Assert.ThrowsAnyAsync<Exception>(async () => await hangingProcess.Start());
            await hangingProcess.Kill();
            await hangingProcess.ReleaseResources();
        }

        [Fact]
        public async Task TestReleaseResources()
        {
            IProcess hangingProcess = new Yams.Process.Process(_hangingExePath, "");
            await hangingProcess.ReleaseResources(); // should do nothing
            await hangingProcess.Start();
            Assert.True(hangingProcess.IsRunning);

            await Assert.ThrowsAnyAsync<Exception>(async () => await hangingProcess.ReleaseResources());

            await hangingProcess.Kill();
            await hangingProcess.ReleaseResources();
        }

        [Fact]
        public async Task TestThatExitedEventIsFired()
        {
            IProcess suicidalProcess = new Yams.Process.Process(_suicidalExePath, "");
            bool exitedFired = false;
            suicidalProcess.Exited += (sender, args) =>
            {
                exitedFired = true;
            };
            await suicidalProcess.Start();
            Assert.True(await ProcessUtils.SpinWaitForExit(suicidalProcess, 5));
            Assert.True(exitedFired);
            await suicidalProcess.ReleaseResources();
        }

        [Fact]
        public void TestProperties()
        {
            const string exeArgs = "arggg";
            IProcess suicidalProcess = new Yams.Process.Process(_suicidalExePath, exeArgs);
            Assert.Equal(_suicidalExePath, suicidalProcess.ExePath);
            Assert.Equal(exeArgs, suicidalProcess.ExeArgs);
        }
    }
}
