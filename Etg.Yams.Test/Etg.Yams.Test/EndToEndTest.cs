using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Storage;
using Etg.Yams.Test.Storage;
using Etg.Yams.Test.Utils;
using Etg.Yams.Update;
using Etg.Yams.Update.Fakes;
using Etg.Yams.Utils;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Etg.Yams.Test
{
    [TestClass]
    public class EndToEndTest
    {
        private string _dataRootPath;
        private string _testDirPath;
        private string _applicationsInstallPath;
        private string _deploymentDirPath;
        private YamsEntryPoint _yamsEntryPoint;
        private UnityContainer _diContainer;

        [TestInitialize]
        public void TestInitialize()
        {
            _dataRootPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "EndToEndTest");
            _testDirPath = Path.Combine(Directory.GetCurrentDirectory(), "EndToEndTest");
            _deploymentDirPath = Path.Combine(_testDirPath, "Deployments");
            _applicationsInstallPath = Path.Combine(_testDirPath, Constants.ApplicationsRootFolderName);

            FileUtils.CopyDir(_dataRootPath, _deploymentDirPath, overwrite: true).Wait();

            CopyTestProcessExeToTestApps();

            _diContainer = new UnityContainer();
            DiModule.RegisterTypes(_diContainer, new YamsConfigBuilder("UseDevelopmentStorage=true", "deploymentId1", "1", "instanceId", _applicationsInstallPath).Build());

            // Replace the IRemoteDirectory default implementation (which uses Azure) with a LocalDirectory implementation
            // so we can use local test data.
            _diContainer.RegisterType<IYamsRepository>(new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c => new LocalYamsRepository(_deploymentDirPath)));

            IUpdateSessionManager updateSessionManager = new StubIUpdateSessionManager
            {
                TryStartUpdateSessionString = applicationId => Task.FromResult(true),
                EndUpdateSessionString = applicationId => Task.FromResult(true)
            };

            // Replace the update session manager with a stub that always starts an update session (since we don't have multiple instances
            // updating in a test)
            _diContainer.RegisterInstance(updateSessionManager);

            // we don't start yams because that will make it update based on a timer that is hard to manage in a test. Instead, we call
            // applicationUpdateManager.CheckForUpdates() manually in a test.
            _yamsEntryPoint = new YamsEntryPoint(_diContainer);
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

        [TestCleanup]
        public void TestCleanup()
        {
            _yamsEntryPoint.Stop().Wait();
            DeleteDirectory(_testDirPath);
        }

        [TestMethod]
        public async Task TestThatApplicationsAreLoadedAtStartup()
        {
            IApplicationUpdateManager applicationUpdateManager = _diContainer.Resolve<IApplicationUpdateManager>();
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
        [TestMethod]
        public async Task TestMultipleUpdates()
        {
            IApplicationUpdateManager applicationUpdateManager = _diContainer.Resolve<IApplicationUpdateManager>();
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
            IApplicationPool applicationPool = _diContainer.Resolve<IApplicationPool>();
            Assert.IsTrue(applicationPool.HasApplication(appIdentity), string.Format("App {0} should be running!", appIdentity));
            string processOutput = TestUtils.GetTestApplicationOutput(_applicationsInstallPath, appIdentity);
            Assert.AreEqual("TestProcess.exe foo1 foo2", processOutput);
        }

        public void AssertThatApplicationIsNotRunning(AppIdentity appIdentity)
        {
            IApplicationPool applicationPool = _diContainer.Resolve<IApplicationPool>();
            Assert.IsFalse(applicationPool.HasApplication(appIdentity), string.Format("App {0} should not be running!", appIdentity));
            Assert.IsFalse(Directory.Exists(Path.Combine(_applicationsInstallPath, ApplicationUtils.GetApplicationRelativePath(appIdentity))));
        }

        public void AssertThatNumberOfApplicationsRunningIs(int count)
        {
            IApplicationPool applicationPool = _diContainer.Resolve<IApplicationPool>();
            Assert.AreEqual(applicationPool.Applications.Count(), count);
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
