using System;
using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Process;
using Etg.Yams.Test.stubs;
using Etg.Yams.Test.Utils;
using Etg.Yams.Utils;
using Xunit;
using Etg.Yams.Application.Fakes;

namespace Etg.Yams.Test.Application
{
    public class ApplicationPoolTestFixture : IDisposable
    {
        public ApplicationPool ApplicationPool { get; private set; }

        private string _dataRootPath;
        public string ApplicationsRootPath { get; private set; }
        public IApplicationFactory ApplicationFactory { get; private set; }
        private string _testDirPath;
        private const string DeploymentId = "testDeploymentId";
        private const string InstanceId = "testInstanceId";

        public ApplicationPoolTestFixture()
        {
            _dataRootPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "ApplicationPool");
            _testDirPath = Path.Combine(Directory.GetCurrentDirectory(), "ApplicationPoolTest");
            ApplicationsRootPath = Path.Combine(_testDirPath, "Applications");

            FileUtils.CopyDir(_dataRootPath, ApplicationsRootPath, overwrite: true).Wait();

            const string exeName = "TestProcess.exe";
            string[] testAppsRelPath =
            {
                Path.Combine("test.myapp", "1.0.0"),
                Path.Combine("test.myapp", "1.0.1"),
            };

            foreach (string testAppRelPath in testAppsRelPath)
            {
                TestUtils.CopyExe(exeName, Path.Combine(ApplicationsRootPath, testAppRelPath));
            }

            ApplicationFactory =
                new ConfigurableApplicationFactory(new ApplicationConfigParser(new ApplicationConfigSymbolResolver(DeploymentId, InstanceId)),
                    new SelfRestartingProcessFactory(0), new ProcessStopper(0));
            ApplicationPool = new ApplicationPool();
        }

        public void Dispose()
        {
            if (ApplicationPool != null)
            {
                ApplicationPool.Shutdown().Wait();
            }
        }
    }

    public class ApplicationPoolTest : IClassFixture<ApplicationPoolTestFixture>
    {
        private ApplicationPool _applicationPool;
        private IApplicationFactory _applicationFactory;
        private string _applicationsRootPath;

        public ApplicationPoolTest(ApplicationPoolTestFixture fixture)
        {
            _applicationPool = fixture.ApplicationPool;
            _applicationFactory = fixture.ApplicationFactory;
            _applicationsRootPath = fixture.ApplicationsRootPath;
        }
        
        [Fact]
        public async Task TestAddApplication()
        {
            AppIdentity appIdentity = new AppIdentity("test.myapp", new Version(1, 0, 0));
            await AddApplication(appIdentity);

            Assert.NotNull(_applicationPool.GetApplication(appIdentity));
            Assert.Equal("TestProcess.exe foo1 foo2", GetOutput(appIdentity));
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
        
        [Fact]
        public async Task TestThatAddExistingApplicationThrowsAnException()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () => {
                AppIdentity appIdentity = new AppIdentity("test.myapp", new Version(1, 0, 0));
                await AddApplication(appIdentity);
                await AddApplication(appIdentity);
            });            
        }

        [Fact]
        public void TestThatGetApplicationReturnsNullIfApplicationDoesntExist()
        {
            AppIdentity appIdentity = new AppIdentity("test.myapp", new Version(1, 0, 0));
            Assert.Null(_applicationPool.GetApplication(appIdentity));
        }

        [Fact]
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

            Assert.True(_applicationPool.HasApplication(appIdentity));
            Assert.Equal(1, startCallCount);
            Assert.Equal(0, stopCallCount);

            await _applicationPool.RemoveApplication(appIdentity);
            Assert.Equal(1, stopCallCount);
        }

        [Fact]
        public async Task TestThatAnExceptionIsThrownIfApplicationFailsToStart()
        {
            await Assert.ThrowsAsync<Exception>(async () => {
                IApplication application = new StubIApplication
                {
                    Start = () => Task.FromResult(false),
                    IdentityGet = () => new AppIdentity("test.myapp", new Version(1, 0, 0)),
                };
                await _applicationPool.AddApplication(application);
            });            
        }

        [Fact]
        public async Task TestThatExitedApplicationsAreRemoved()
        {
            AppIdentity appIdentity = new AppIdentity("test.myapp", new Version(1, 0, 0));
            ApplicationStub application = new ApplicationStub(appIdentity, "path");

            await _applicationPool.AddApplication(application);
            Assert.True(_applicationPool.HasApplication(appIdentity));

            application.Fail();
            Assert.False(_applicationPool.HasApplication(appIdentity));
        }

        [Fact]
        public async Task TestThatRemoveNonExistingApplicationThrowsAnException()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                AppIdentity appIdentity = new AppIdentity("test.myapp", new Version(1, 0, 0));
                await _applicationPool.RemoveApplication(appIdentity);
            });
        }

        private string GetOutput(AppIdentity appIdentity)
        {
            return TestUtils.GetTestApplicationOutput(_applicationsRootPath, appIdentity);
        }
    }
}
