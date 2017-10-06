using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Test.Storage;
using Etg.Yams.Test.Utils;
using Etg.Yams.Update;
using Etg.Yams.Utils;
using Xunit;
using Autofac;
using Etg.Yams.Json;
using Etg.Yams.Storage.Config;
using Newtonsoft.Json.Serialization;
using Semver;
using Etg.Yams.Storage;
using Etg.Yams.Install;
using System.Collections.Generic;

namespace Etg.Yams.Test
{
    public class EndToEndTest : IDisposable
    {
        private readonly string _dataRootPath;
        private readonly string _testDirPath;
        private readonly string _applicationsInstallPath;
        private readonly string _deploymentDirPath;
        private IYamsService _yamsService;
        private YamsDiModule _yamsDiModule;

        public EndToEndTest()
        {
            _dataRootPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "EndToEndTest");
            _testDirPath = Path.Combine(Directory.GetCurrentDirectory(), "EndToEndTest");
            _deploymentDirPath = Path.Combine(_testDirPath, "Deployments");
            _applicationsInstallPath = Path.Combine(_testDirPath, "applications");

            FileUtils.CopyDir(_dataRootPath, _deploymentDirPath, overwrite: true).Wait();

            CopyTestProcessExeToTestApps();
        }

        private void InitializeYamsService(YamsConfig yamsConfig)
        {
            ContainerBuilder builder = InitializeContainerBuilder(yamsConfig);
            InitializeYamsService(builder.Build());
        }

        private void InitializeYamsService(IContainer container)
        {
            _yamsDiModule = new YamsDiModule(container);
            _yamsService = _yamsDiModule.YamsService;
        }

        private ContainerBuilder InitializeContainerBuilder(YamsConfig yamsConfig)
        {
            IUpdateSessionManager updateSessionManager = new StubIUpdateSessionManager()
                .TryStartUpdateSession(applicationId => Task.FromResult(true))
                .EndUpdateSession(applicationId => Task.FromResult(true));

            IDeploymentRepository deploymentRepository = new LocalDeploymentRepository(_deploymentDirPath,
                new JsonDeploymentConfigSerializer(new JsonSerializer(new DiagnosticsTraceWriter())));
            
            return YamsDiModule.RegisterTypes(yamsConfig, deploymentRepository, updateSessionManager);
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
                Path.Combine("test.app2", "2.0.0-beta"),
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

        /// <summary>
        /// This test replaces the content DeploymentConfig.json file with DeploymentConfigUpdate.json file.
        /// Several updates are involved:
        /// - test.app1.1.0.0 is removed
        /// - test.app1.1.0.1 is added
        /// - test.app2.1.1.0 deployment id is changed (i.e. the app is removed)
        /// - test.app2.2.0.0-beta is still there
        /// - test.app3.1.0.0 is added
        /// - test.app3.1.1.0 is still there
        /// - test.app4.1.0.0 deployment id now matches the fabric deployment id (i.e. the app is added)
        /// </summary>
        [Fact]
        public async Task TestMultipleUpdates()
        {
            var yamsConfig = new YamsConfigBuilder("clusterId1", "1", "instanceId",
                _applicationsInstallPath).SetShowApplicationProcessWindow(false).Build();

            InitializeYamsService(yamsConfig);

            IApplicationUpdateManager applicationUpdateManager = _yamsDiModule.Container.Resolve<IApplicationUpdateManager>();
            await applicationUpdateManager.CheckForUpdates();

            AssertThatApplicationIsRunning(new AppIdentity("test.app1", new SemVersion(1, 0, 0)), "TestProcess");
            AssertThatApplicationIsRunning(new AppIdentity("test.app2", new SemVersion(1, 1, 0)), "TestProcess");
            AssertThatApplicationIsRunning(new AppIdentity("test.app2", new SemVersion(2, 0, 0, "beta")), "TestProcess");
            AssertThatApplicationIsRunning(new AppIdentity("test.app3", new SemVersion(1, 1, 0)), "TestProcess");

            UploadDeploymentConfig("DeploymentConfigUpdate.json");

            await applicationUpdateManager.CheckForUpdates();

            AssertThatApplicationIsNotRunning(new AppIdentity("test.app1", new SemVersion(1, 0, 0)));
            AssertThatApplicationIsNotRunning(new AppIdentity("test.app2", new SemVersion(1, 1, 0)));

            AssertThatApplicationIsRunning(new AppIdentity("test.app1", new SemVersion(1, 0, 1)), "TestProcess");
            AssertThatApplicationIsRunning(new AppIdentity("test.app2", new SemVersion(2, 0, 0,"beta")), "TestProcess");
            AssertThatApplicationIsRunning(new AppIdentity("test.app3", new SemVersion(1, 0, 0)), "TestProcess");
            AssertThatApplicationIsRunning(new AppIdentity("test.app3", new SemVersion(1, 1, 0)), "TestProcess");
            AssertThatApplicationIsRunning(new AppIdentity("test.app4", new SemVersion(1, 0, 0)), "TestProcess");

            AssertThatNumberOfApplicationsRunningIs(5);
        }

        private void UploadDeploymentConfig(string deploymentConfigFileName)
        {
            File.Copy(Path.Combine(_dataRootPath, deploymentConfigFileName), Path.Combine(_deploymentDirPath,
                "DeploymentConfig.json"), overwrite: true);
        }

        [Fact]
        public async Task TestThatClusterPropertiesAreUsedToMatchDeployments()
        {
            UploadDeploymentConfig("DeploymentConfigWithProperties.json");
            var yamsConfig = new YamsConfigBuilder("clusterId1", "1", "instanceId",
                _applicationsInstallPath).SetShowApplicationProcessWindow(false)
                .AddClusterProperty("NodeType", "Test")
                .AddClusterProperty("Region", "East").Build();

            var installedApps = new List<AppInstallConfig>();
            var applicationInstallerStub = new StubIApplicationInstaller().Install(
                (config) => 
                {
                    installedApps.Add(config);
                    return Task.CompletedTask;
                });

            ContainerBuilder builder = InitializeContainerBuilder(yamsConfig);
            builder.RegisterInstance<IApplicationInstaller>(applicationInstallerStub);
            InitializeYamsService(builder.Build());

            IApplicationUpdateManager applicationUpdateManager = _yamsDiModule.Container.Resolve<IApplicationUpdateManager>();
            await applicationUpdateManager.CheckForUpdates();
            
            Assert.Equal(2, installedApps.Count);
            Assert.True(installedApps.Any(config => config.AppIdentity == new AppIdentity("test.app1", "1.0.0")));
            Assert.True(installedApps.Any(config => config.AppIdentity == new AppIdentity("test.app2", "2.0.0-beta")));

            AppInstallConfig appInstallConfig = installedApps.Find(config => config.AppIdentity.Id == "test.app1");
            Assert.Equal(new AppIdentity("test.app1", new SemVersion(1, 0, 0)), appInstallConfig.AppIdentity);
            Assert.True(appInstallConfig.Properties.ContainsKey("NodeType"));
            Assert.Equal("Test", appInstallConfig.Properties["NodeType"]);
            Assert.True(appInstallConfig.Properties.ContainsKey("Region"));
            Assert.Equal("East", appInstallConfig.Properties["Region"]);
        }

        [Fact]
        public async Task TestApplicationWithHeartBeat()
        {
            await RunHeartBeatTest(TimeSpan.FromSeconds(3));
        }

        /// <summary>
        /// The current behaviour is to not terminate the app if it has slow (or no) heart beat but to only log errors.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task TestApplicationWithHeartBeatTimeout()
        {
            await RunHeartBeatTest(TimeSpan.FromSeconds(1));
        }

        private async Task RunHeartBeatTest(TimeSpan heartBeatTimeout)
        {
            await CopyAppBinariesToAppDeploymentDir("HeartBeatApp", "HeartBeatProcess", "1.0.0");
            UploadDeploymentConfig("DeploymentConfigHeartBeatApp.json");

            var yamsConfig = new YamsConfigBuilder("clusterId1", "1", "instanceId",
                    _applicationsInstallPath)
                .SetAppHeartBeatTimeout(heartBeatTimeout)
                .SetShowApplicationProcessWindow(false).Build();

            InitializeYamsService(yamsConfig);

            IApplicationUpdateManager applicationUpdateManager = _yamsDiModule.Container.Resolve<IApplicationUpdateManager>();
            await applicationUpdateManager.CheckForUpdates();

            AssertThatApplicationIsRunning(new AppIdentity("HeartBeatApp", new SemVersion(1, 0, 0)));
            // wait for a bit to make sure heart beat messages are not failing
            await Task.Delay(1000);
            AssertThatApplicationIsRunning(new AppIdentity("HeartBeatApp", new SemVersion(1, 0, 0)));
        }

        [Fact]
        public async Task TestApplicationWithMonitoredInitialization()
        {
            await CopyAppBinariesToAppDeploymentDir("MonitorInitApp", "MonitorInitProcess", "1.0.0");
            UploadDeploymentConfig("DeploymentConfigMonitorInitApp.json");

            var yamsConfig = new YamsConfigBuilder("clusterId1", "1", "instanceId",
                _applicationsInstallPath).SetAppInitTimeout(TimeSpan.FromSeconds(10))
                .SetShowApplicationProcessWindow(false).Build();

            InitializeYamsService(yamsConfig);

            IApplicationUpdateManager applicationUpdateManager = _yamsDiModule.Container.Resolve<IApplicationUpdateManager>();
            await applicationUpdateManager.CheckForUpdates();

            AssertThatApplicationIsRunning(new AppIdentity("MonitorInitApp", new SemVersion(1, 0, 0)));
        }

        [Fact]
        public async Task TestApplicationWithMonitoredInitializationTimeout()
        {
            await CopyAppBinariesToAppDeploymentDir("MonitorInitApp", "MonitorInitProcess", "1.0.0");
            UploadDeploymentConfig("DeploymentConfigMonitorInitApp.json");

            var yamsConfig = new YamsConfigBuilder("clusterId1", "1", "instanceId",
                    _applicationsInstallPath).SetAppInitTimeout(TimeSpan.FromSeconds(1))
                .SetShowApplicationProcessWindow(false).Build();

            InitializeYamsService(yamsConfig);

            IApplicationUpdateManager applicationUpdateManager = _yamsDiModule.Container.Resolve<IApplicationUpdateManager>();
            await applicationUpdateManager.CheckForUpdates();

            AssertThatApplicationIsNotRunning(new AppIdentity("MonitorInitApp", new SemVersion(1, 0, 0)));
        }

        [Fact]
        public async Task TestApplicationWithGracefulShutdown()
        {
            await RunGracefulShutdownTest(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task TestApplicationWithGracefulShutdownTimeout()
        {
            await RunGracefulShutdownTest(TimeSpan.FromSeconds(1));
        }

        private async Task RunGracefulShutdownTest(TimeSpan gracefulShutdownTimeout)
        {
            await CopyAppBinariesToAppDeploymentDir("GracefulShutdownApp", "GracefullShutdownProcess", "1.0.0");
            UploadDeploymentConfig("DeploymentConfigGracefulShutdownApp.json");

            var yamsConfig = new YamsConfigBuilder("clusterId1", "1", "instanceId",
                    _applicationsInstallPath).SetAppGracefulShutdownTimeout(gracefulShutdownTimeout)
                .SetShowApplicationProcessWindow(false).Build();

            InitializeYamsService(yamsConfig);

            IApplicationUpdateManager applicationUpdateManager = _yamsDiModule.Container.Resolve<IApplicationUpdateManager>();
            await applicationUpdateManager.CheckForUpdates();
            AssertThatApplicationIsRunning(new AppIdentity("GracefulShutdownApp", new SemVersion(1, 0, 0)));

            UploadDeploymentConfig("DeploymentConfigNoApps.json");
            await applicationUpdateManager.CheckForUpdates();
            AssertThatApplicationIsNotRunning(new AppIdentity("GracefulShutdownApp", new SemVersion(1, 0, 0)));
        }

        [Fact]
        public async Task TestFullIpcApp()
        {
            await CopyAppBinariesToAppDeploymentDir("FullIpcApp", "FullIpcProcess", "1.0.0");
            UploadDeploymentConfig("DeploymentConfigFullIpcApp.json");

            var yamsConfig = new YamsConfigBuilder("clusterId1", "1", "instanceId",
                    _applicationsInstallPath).SetShowApplicationProcessWindow(false).Build();

            InitializeYamsService(yamsConfig);

            IApplicationUpdateManager applicationUpdateManager = _yamsDiModule.Container.Resolve<IApplicationUpdateManager>();
            await applicationUpdateManager.CheckForUpdates();
            AssertThatApplicationIsRunning(new AppIdentity("FullIpcApp", new SemVersion(1, 0, 0)));

            await Task.Delay(5000);

            UploadDeploymentConfig("DeploymentConfigNoApps.json");
            await applicationUpdateManager.CheckForUpdates();
            AssertThatApplicationIsNotRunning(new AppIdentity("FullIpcApp", new SemVersion(1, 0, 0)));
        }

        public void AssertThatApplicationIsRunning(AppIdentity appIdentity, string exeName = "")
        {
            if (String.IsNullOrEmpty(exeName))
            {
                exeName = appIdentity.Id;
            }
            IApplicationPool applicationPool = _yamsDiModule.Container.Resolve<IApplicationPool>();
            Assert.True(applicationPool.HasApplication(appIdentity), $"App {appIdentity} should be running!");
            string processOutput = TestUtils.GetTestApplicationOutput(_applicationsInstallPath, appIdentity, exeName);
            Assert.Equal($"{exeName}.exe foo1 foo2", processOutput);
        }

        private async Task CopyAppBinariesToAppDeploymentDir(string appName, string processName, string version)
        {
            await FileUtils.CopyDir(
                srcPath: Path.Combine(Directory.GetCurrentDirectory(), "Data", processName),
                destPath: Path.Combine(Directory.GetCurrentDirectory(), "EndToEndTest", "Deployments", appName, version),
                overwrite: true);
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
