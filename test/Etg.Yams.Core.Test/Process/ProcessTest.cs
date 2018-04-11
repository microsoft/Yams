using System;
using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Process;
using Etg.Yams.Test.Utils;
using Etg.Yams.Utils;
using Xunit;

namespace Etg.Yams.Test.Process
{
    public class ProcessTestFixture
    {
        public readonly string SuicidalExePath;
        public readonly string HangingExePath;

        public readonly AppIdentity HangingProcessIdentity = new AppIdentity("SuicidalProcess", "2.0.0");
        public readonly AppIdentity SuicidalProcessIdentity = new AppIdentity("HangingProcess", "1.1.4-alpha3");
        const string SuicidalProcessExeName = "SuicidalProcess.exe";
        const string HangingProcessExeName = "HangingProcess.exe";

        public ProcessTestFixture()
        {
            var testDirPath = Path.Combine(Directory.GetCurrentDirectory(), "ProcessTest");
            SuicidalExePath = Path.Combine(testDirPath, SuicidalProcessExeName);
            HangingExePath = Path.Combine(testDirPath, HangingProcessExeName);

            Directory.CreateDirectory(testDirPath);
            TestUtils.CopyExesTestDir(testDirPath);
        }
    }

    public class ProcessTest : IClassFixture<ProcessTestFixture>
    {
        readonly ProcessTestFixture _fixture;
        public ProcessTest(ProcessTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task TestIsRunning()
        {
            IProcess hangingProcess = new Yams.Process.Process(_fixture.HangingProcessIdentity, _fixture.HangingExePath, false);
            Assert.False(hangingProcess.IsRunning);
            await hangingProcess.Start(string.Empty);
            Assert.True(hangingProcess.IsRunning);
            await hangingProcess.Kill();
            Assert.True(await ProcessUtils.SpinWaitForExit(hangingProcess, 5));
            Assert.False(hangingProcess.IsRunning);
        }

        [Fact]
        public async Task TestThatProcessCannotBeStartedMoreThanOnce()
        {
            IProcess hangingProcess = new Yams.Process.Process(_fixture.HangingProcessIdentity, _fixture.HangingExePath, false);
            await hangingProcess.Start(string.Empty);
            Assert.True(hangingProcess.IsRunning);

            await Assert.ThrowsAnyAsync<Exception>(async () => await hangingProcess.Start(string.Empty));
            await hangingProcess.Kill();
            await hangingProcess.ReleaseResources();
        }

        [Fact]
        public async Task TestReleaseResources()
        {
            IProcess hangingProcess = new Yams.Process.Process(_fixture.HangingProcessIdentity, _fixture.HangingExePath, false);
            await hangingProcess.ReleaseResources(); // should do nothing
            await hangingProcess.Start(string.Empty);
            Assert.True(hangingProcess.IsRunning);

            await Assert.ThrowsAnyAsync<Exception>(async () => await hangingProcess.ReleaseResources());

            await hangingProcess.Kill();
            await hangingProcess.ReleaseResources();
        }

        [Fact(Skip = "For some reason this test is flaky on CI builds. Disabling for now..")]
        public async Task TestThatExitedEventIsFired()
        {
            IProcess suicidalProcess = new Yams.Process.Process(_fixture.SuicidalProcessIdentity, _fixture.SuicidalExePath, false);
            bool exitedFired = false;
            suicidalProcess.Exited += (sender, args) =>
            {
                exitedFired = true;
            };
            await suicidalProcess.Start(string.Empty);
            Assert.True(await ProcessUtils.SpinWaitForExit(suicidalProcess, 5));
            Assert.True(exitedFired);
            await suicidalProcess.ReleaseResources();
        }

        [Fact]
        public async Task TestProperties()
        {
            const string exeArgs = "arggg";
            IProcess suicidalProcess = new Yams.Process.Process(_fixture.SuicidalProcessIdentity, _fixture.SuicidalExePath, false);
            await suicidalProcess.Start(exeArgs);
            Assert.Equal(_fixture.SuicidalExePath, suicidalProcess.ExePath);
            Assert.Equal($"{exeArgs} --AppName {_fixture.SuicidalProcessIdentity.Id} --AppVersion {_fixture.SuicidalProcessIdentity.Version}", suicidalProcess.ExeArgs);
        }
    }
}
