using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Test.Storage;
using Etg.Yams.Test.Utils;
using Etg.Yams.Update;
using Etg.Yams.Update.Fakes;
using Etg.Yams.Utils;
using Xunit;
using Autofac;

namespace Etg.Yams.Test
{
    public class EndToEndTest : IDisposable
    {
        private readonly string _dataRootPath;
        private readonly string _testDirPath;
        private readonly string _applicationsInstallPath;
        private readonly string _deploymentDirPath;
        private readonly IYamsService _yamsService;
        private YamsDiModule _yamsDiModule;

        public EndToEndTest()
        {
            _dataRootPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "EndToEndTest");
            _testDirPath = Path.Combine(Directory.GetCurrentDirectory(), "EndToEndTest");
            _deploymentDirPath = Path.Combine(_testDirPath, "Deployments");
            _applicationsInstallPath = Path.Combine(_testDirPath, "applications");

            FileUtils.CopyDir(_dataRootPath, _deploymentDirPath, overwrite: true).Wait();

            CopyTestProcessExeToTestApps();

            var yamsConfig = new YamsConfigBuilder("deploymentId1", "1", "instanceId",
                _applicationsInstallPath).SetShowApplicationProcessWindow(false).Build();

            IUpdateSessionManager updateSessionManager = new StubIUpdateSessionManager
            {
                TryStartUpdateSessionString = applicationId => Task.FromResult(true),
                EndUpdateSessionString = applicationId => Task.FromResult(true)
            };

            _yamsDiModule = new YamsDiModule(yamsConfig, new LocalDeploymentRepository(_deploymentDirPath), updateSessionManager);
            _yamsService = _yamsDiModule.YamsService;
        }

        private void CopyTestProcessExeToTestApps()
        {
            // The exe will be created at compile time. The code below copies the exe to each
            // of the test apps used for testing.
            const string exeName = "TestProcess.exe";
            string[] testAppsRelPath =
            {
                Path.Combine("test.app1", "1.0.0"),
                Path.Combine("test.app1", "1.0.1"),
                Path.Combine("test.app2", "1.1.0"),
                Path.Combine("test.app2", "2.0.0"),
                Path.Combine("test.app3", "1.0.0"),
                Path.Combine("test.app3", "1.1.0"),
                Path.Combine("test.app4", "1.0.0"),
            };

            foreach (string testAppRelPath in testAppsRelPath)
            {
                TestUtils.CopyExe(exeName, Path.Combine(_deploymentDirPath, testAppRelPath));
            }
        }

        
        public void Dispose()
        {
            _yamsService.Stop().Wait();
            DeleteDirectory(_testDirPath);
        }

        [Fact]
        public async Task TestThatApplicationsAreLoadedAtStartup()
        {
            IApplicationUpdateManager applicationUpdateManager = _yamsDiModule.Container.Resolve<IApplicationUpdateManager>();
            await applicationUpdateManager.CheckForUpdates();
            
            AssertThatApplicationIsRunning(new AppIdentity("test.app1", new Version(1, 0, 0)));
            AssertThatApplicationIsRunning(new AppIdentity("test.app2", new Version(1, 1, 0)));
            AssertThatApplicationIsRunning(new AppIdentity("test.app2", new Version(2, 0, 0)));
            AssertThatApplicationIsRunning(new AppIdentity("test.app3", new Version(1, 1, 0)));

            AssertThatApplicationIsNotRunning(new AppIdentity("test.app4", new Version(1, 0, 0)));

            AssertThatNumberOfApplicationsRunningIs(4);
        }

        /// <summary>
        /// This test replaces the content DeploymentConfig.json file with DeploymentConfigUpdate.json file.
        /// Several updates are involved:
        /// - test.app1.1.0.0 is removed
        /// - test.app1.1.0.1 is added
        /// - test.app2.1.1.0 deployment id is changed (i.e. the app is removed)
        /// - test.app2.2.0.0 is still there
        /// - test.app3.1.0.0 is added
        /// - test.app3.1.1.0 is still there
        /// - test.app4.1.0.0 deployment id now matches the fabric deployment id (i.e. the app is added)
        /// </summary>
        [Fact]
        public async Task TestMultipleUpdates()
        {
            IApplicationUpdateManager applicationUpdateManager = _yamsDiModule.Container.Resolve<IApplicationUpdateManager>();
            await applicationUpdateManager.CheckForUpdates();

            File.Copy(Path.Combine(_dataRootPath, "DeploymentConfigUpdate.json"), Path.Combine(_deploymentDirPath, "DeploymentConfig.json"), overwrite:true);

            await applicationUpdateManager.CheckForUpdates();

            AssertThatApplicationIsNotRunning(new AppIdentity("test.app1", new Version(1, 0, 0)));
            AssertThatApplicationIsNotRunning(new AppIdentity("test.app2", new Version(1, 1, 0)));

            AssertThatApplicationIsRunning(new AppIdentity("test.app1", new Version(1, 0, 1)));
            AssertThatApplicationIsRunning(new AppIdentity("test.app2", new Version(2, 0, 0)));
            AssertThatApplicationIsRunning(new AppIdentity("test.app3", new Version(1, 0, 0)));
            AssertThatApplicationIsRunning(new AppIdentity("test.app3", new Version(1, 1, 0)));
            AssertThatApplicationIsRunning(new AppIdentity("test.app4", new Version(1, 0, 0)));

            AssertThatNumberOfApplicationsRunningIs(5);
        }

        public void AssertThatApplicationIsRunning(AppIdentity appIdentity)
        {
            IApplicationPool applicationPool = _yamsDiModule.Container.Resolve<IApplicationPool>();
            Assert.True(applicationPool.HasApplication(appIdentity), $"App {appIdentity} should be running!");
            string processOutput = TestUtils.GetTestApplicationOutput(_applicationsInstallPath, appIdentity);
            Assert.Equal("TestProcess.exe foo1 foo2", processOutput);
        }

        public void AssertThatApplicationIsNotRunning(AppIdentity appIdentity)
        {
            IApplicationPool applicationPool = _yamsDiModule.Container.Resolve<IApplicationPool>();
            Assert.False(applicationPool.HasApplication(appIdentity), $"App {appIdentity} should not be running!");
            Assert.False(Directory.Exists(Path.Combine(_applicationsInstallPath, ApplicationUtils.GetApplicationRelativePath(appIdentity))));
        }

        public void AssertThatNumberOfApplicationsRunningIs(int count)
        {
            IApplicationPool applicationPool = _yamsDiModule.Container.Resolve<IApplicationPool>();
            Assert.Equal(applicationPool.Applications.Count(), count);
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

    }
}
