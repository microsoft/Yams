using System;
using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Application.Fakes;
using Etg.Yams.Process;
using Etg.Yams.Test.stubs;
using Etg.Yams.Test.Utils;
using Etg.Yams.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Etg.Yams.Test.Application
{
    [TestClass]
    public class ApplicationPoolTest
    {
        private ApplicationPool _applicationPool;
        private static string _dataRootPath;
        private static string _applicationsRootPath;
        private IApplicationFactory _applicationFactory;
        private static string _testDirPath;
        private const string DeploymentId = "testDeploymentId";
        private const string InstanceId = "testInstanceId";

        public ApplicationPoolTest()
        {
            _dataRootPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "ApplicationPool");
            _testDirPath = Path.Combine(Directory.GetCurrentDirectory(), "ApplicationPoolTest");
            _applicationsRootPath = Path.Combine(_testDirPath, "Applications");
        }

        [TestInitialize]
        public void Initialize()
        {
            FileUtils.CopyDir(_dataRootPath, _applicationsRootPath, overwrite: true).Wait();

            const string exeName = "TestProcess.exe";
            string[] testAppsRelPath =
            {
                Path.Combine("test.myapp", "1.0.0"),
                Path.Combine("test.myapp", "1.0.1"),
            };

            foreach (string testAppRelPath in testAppsRelPath)
            {
                TestUtils.CopyExe(exeName, Path.Combine(_applicationsRootPath, testAppRelPath));
            }

            _applicationFactory =
                new ConfigurableApplicationFactory(new ApplicationConfigParser(new ApplicationConfigSymbolResolver(DeploymentId, InstanceId)),
                    new SelfRestartingProcessFactory(0), new ProcessStopper(0));
            _applicationPool = new ApplicationPool();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _applicationPool.Shutdown().Wait();
        }

        [TestMethod]
        public async Task TestAddApplication()
        {
            AppIdentity appIdentity = new AppIdentity("test.myapp", new Version(1, 0, 0));
            await AddApplication(appIdentity);

            Assert.IsNotNull(_applicationPool.GetApplication(appIdentity));
            Assert.AreEqual("TestProcess.exe foo1 foo2", GetOutput(appIdentity));
        }

        private async Task AddApplication(AppIdentity appIdentity)
        {
            IApplication application = await _applicationFactory.CreateApplication(appIdentity, GetApplicationPath(appIdentity));
            await _applicationPool.AddApplication(application);
        }

        private string GetApplicationPath(AppIdentity appIdentity)
        {
            return Path.Combine(_applicationsRootPath, appIdentity.Id,
                appIdentity.Version.ToString());
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]

        public async Task TestThatAddExistingApplicationThrowsAnException()
        {
            AppIdentity appIdentity = new AppIdentity("test.myapp", new Version(1, 0, 0));
            await AddApplication(appIdentity);
            await AddApplication(appIdentity);
        }

        [TestMethod]
        public void TestThatGetApplicationReturnsNullIfApplicationDoesntExist()
        {
            AppIdentity appIdentity = new AppIdentity("test.myapp", new Version(1, 0, 0));
            Assert.IsNull(_applicationPool.GetApplication(appIdentity));
        }

        [TestMethod]
        public async Task TestRemoveApplication()
        {
            int startCallCount = 0;
            int stopCallCount = 0;
            AppIdentity appIdentity = new AppIdentity("test.myapp", new Version(1, 0, 0));
            IApplication application = new StubIApplication()
            {
                Start = () =>
                {
                    startCallCount++;
                    return Task.FromResult(true);
                },
                Stop = () =>
                {
                    stopCallCount++;
                    return Task.FromResult(true);
                },
                IdentityGet = () => appIdentity
            };

            _applicationPool = new ApplicationPool();

            await _applicationPool.AddApplication(application);

            Assert.IsTrue(_applicationPool.HasApplication(appIdentity));
            Assert.AreEqual(1, startCallCount);
            Assert.AreEqual(0, stopCallCount);

            await _applicationPool.RemoveApplication(appIdentity);
            Assert.AreEqual(1, stopCallCount);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task TestThatAnExceptionIsThrownIfApplicationFailsToStart()
        {
            IApplication application = new StubIApplication
            {
                Start = () => Task.FromResult(false),
                IdentityGet = () => new AppIdentity("test.myapp", new Version(1, 0, 0)),
            };
            await _applicationPool.AddApplication(application);
        }

        [TestMethod]
        public async Task TestThatExitedApplicationsAreRemoved()
        {
            AppIdentity appIdentity = new AppIdentity("test.myapp", new Version(1, 0, 0));
            ApplicationStub application = new ApplicationStub(appIdentity, "path");

            await _applicationPool.AddApplication(application);
            Assert.IsTrue(_applicationPool.HasApplication(appIdentity));

            application.Fail();
            Assert.IsFalse(_applicationPool.HasApplication(appIdentity));
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public async Task TestThatRemoveNonExistingApplicationThrowsAnException()
        {
            AppIdentity appIdentity = new AppIdentity("test.myapp", new Version(1, 0, 0));
            await _applicationPool.RemoveApplication(appIdentity);
        }

        private string GetOutput(AppIdentity appIdentity)
        {
            return TestUtils.GetTestApplicationOutput(_applicationsRootPath, appIdentity);
        }
    }
}
