using System;
using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Install;
using Etg.Yams.Json;
using Etg.Yams.Process;
using Etg.Yams.Test.stubs;
using Etg.Yams.Test.Utils;
using Etg.Yams.Utils;
using Newtonsoft.Json.Serialization;
using Semver;
using Xunit;

namespace Etg.Yams.Test.Application
{
    public class ApplicationPoolTestFixture
    {
        public string ApplicationsRootPath { get; private set; }
        public IApplicationFactory ApplicationFactory { get; private set; }
        private const string ClusterId = "testClusterId";
        private const string InstanceId = "testInstanceId";

        public ApplicationPoolTestFixture()
        {
            var dataRootPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "ApplicationPool");
            var testDirPath = Path.Combine(Directory.GetCurrentDirectory(), "ApplicationPoolTest");
            ApplicationsRootPath = Path.Combine(testDirPath, "Applications");

            FileUtils.CopyDir(dataRootPath, ApplicationsRootPath, overwrite: true).Wait();

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

            YamsConfig config = new YamsConfigBuilder("clusterId", "1", "instanceId", "C:\\")
                .SetShowApplicationProcessWindow(false).SetApplicationRestartCount(0).Build();
            ApplicationFactory =
                new ConfigurableApplicationFactory(new ApplicationConfigParser(
                    new ApplicationConfigSymbolResolver(ClusterId, InstanceId), new JsonSerializer(new DiagnosticsTraceWriter())),
                    new ProcessFactory(config), new ProcessStopper(0));
        }
    }

    public class ApplicationPoolTest : IClassFixture<ApplicationPoolTestFixture>, IDisposable
    {
        private ApplicationPool _applicationPool;
        private readonly IApplicationFactory _applicationFactory;
        private readonly string _applicationsRootPath;

        public ApplicationPoolTest(ApplicationPoolTestFixture fixture)
        {
            _applicationPool = new ApplicationPool();
            _applicationFactory = fixture.ApplicationFactory;
            _applicationsRootPath = fixture.ApplicationsRootPath;
        }

        public void Dispose()
        {
            _applicationPool?.Shutdown().Wait();
        }

        [Fact]
        public async Task TestAddApplication()
        {
            AppIdentity appIdentity = new AppIdentity("test.myapp", new SemVersion(1, 0, 0));
            AppInstallConfig config = new AppInstallConfig(appIdentity);
            await AddApplication(config);

            Assert.NotNull(_applicationPool.GetApplication(appIdentity));
            Assert.Equal("TestProcess.exe foo1 foo2", GetOutput(appIdentity));
        }

        private async Task AddApplication(AppInstallConfig appInstallConfig)
        {
            IApplication application = await _applicationFactory.CreateApplication(appInstallConfig, 
                GetApplicationPath(appInstallConfig.AppIdentity));
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
                AppIdentity appIdentity = new AppIdentity("test.myapp", new SemVersion(1, 0, 0));
                AppInstallConfig config = new AppInstallConfig(appIdentity);
                await AddApplication(config);
                await AddApplication(config);
            });
        }

        [Fact]
        public void TestThatGetApplicationReturnsNullIfApplicationDoesntExist()
        {
            AppIdentity appIdentity = new AppIdentity("test.myapp", new SemVersion(1, 0, 0));
            Assert.Null(_applicationPool.GetApplication(appIdentity));
        }

        [Fact]
        public async Task TestRemoveApplication()
        {
            int startCallCount = 0;
            int stopCallCount = 0;
            AppIdentity appIdentity = new AppIdentity("test.myapp", new SemVersion(1, 0, 0));
            IApplication application = new StubIApplication()
                .Start(() =>
                {
                    startCallCount++;
                    return Task.FromResult(true);
                })
                .Stop(() =>
                {
                    stopCallCount++;
                    return Task.FromResult(true);
                })
                .Identity_Get(() => appIdentity)
                .Dispose(() => { });

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
            await Assert.ThrowsAsync<Exception>(async () =>
            {
	            IApplication application = new StubIApplication()
		            .Start(() => Task.FromResult(false))
		            .Identity_Get(() => new AppIdentity("test.myapp", new SemVersion(1, 0, 0)));
                await _applicationPool.AddApplication(application);
            });
        }

        [Fact]
        public async Task TestThatExitedApplicationsAreRemoved()
        {
            AppIdentity appIdentity = new AppIdentity("test.myapp", new SemVersion(1, 0, 0));
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
                AppIdentity appIdentity = new AppIdentity("test.myapp", new SemVersion(1, 0, 0));
                await _applicationPool.RemoveApplication(appIdentity);
            });
        }

        private string GetOutput(AppIdentity appIdentity)
        {
            return TestUtils.GetTestApplicationOutput(_applicationsRootPath, appIdentity, "TestProcess");
        }
    }
}
