using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Install;
using Etg.Yams.Storage.Config;
using Etg.Yams.Test.stubs;
using Etg.Yams.Update;
using Semver;
using Xunit;

namespace Etg.Yams.Test.Install
{
    public class ApplicationInstallerTest
    {
        private ApplicationInstaller _applicationInstaller;
        private ApplicationPoolStub _applicationPool;
        private readonly string _applicationsRoot;

        public ApplicationInstallerTest()
        {
            _applicationsRoot = Path.Combine(Directory.GetCurrentDirectory(), "ApplicationInstallerTest");
        }

        [Fact]
        public async Task TestInstallApplication()
        {
            _applicationPool = new ApplicationPoolStub();
            IApplicationFactory applicationFactory = new ApplicationFactoryStub();
            _applicationInstaller = new ApplicationInstaller(_applicationsRoot, applicationFactory, _applicationPool);

            AppIdentity appIdentity = new AppIdentity("test.app", new SemVersion(1,0,0));
            await _applicationInstaller.Install(new AppInstallConfig(appIdentity));

            Assert.True(_applicationPool.HasApplicationBeenAdded(appIdentity));
        }

        [Fact]
        public async Task TestRemoveApplication()
        {
            _applicationPool = new ApplicationPoolStub();
            IApplicationFactory applicationFactory = new ApplicationFactoryStub();
            _applicationInstaller = new ApplicationInstaller(_applicationsRoot, applicationFactory, _applicationPool);

            AppIdentity appIdentity = new AppIdentity("test.app", new SemVersion(1, 0, 0));
            _applicationInstaller.Install(new AppInstallConfig(appIdentity)).Wait();

            // make sure the app directory exists because uninstall will try to delete it
            string appPath = Path.Combine(_applicationsRoot, "test.app", "1.0.0");
            if (!Directory.Exists(appPath))
            {
                Directory.CreateDirectory(appPath);
            }
            await _applicationInstaller.UnInstall(appIdentity);

            Assert.False(_applicationPool.HasApplication(appIdentity));
            Assert.False(Directory.Exists(appPath));
        }

        [Fact]
        public async Task TestUpdateApplication()
        {
            IApplicationPool applicationPool = new ApplicationPoolStub();
            IApplicationInstaller applicationInstaller = new ApplicationInstaller(_applicationsRoot, new ApplicationFactoryStub(), applicationPool);

            const string appId = "test.app";
            AppIdentity[] existingApps = { new AppIdentity(appId, new SemVersion(1, 0, 0)), new AppIdentity(appId, new SemVersion(1, 0, 1)) };
            AppIdentity[] newApps = { new AppIdentity(appId, new SemVersion(1, 0, 2)), new AppIdentity(appId, new SemVersion(2, 0, 0)) };

            foreach (var existingApp in existingApps)
            {
                string appPath = Path.Combine(_applicationsRoot, existingApp.Id, existingApp.Version.ToString());
                if (!Directory.Exists(appPath))
                {
                    Directory.CreateDirectory(appPath);
                }
                await applicationInstaller.Install(new AppInstallConfig(existingApp));
                Assert.True(applicationPool.HasApplication(existingApp));
            }

            await applicationInstaller.Update(existingApps, newApps.Select(appIdentity => new AppInstallConfig(appIdentity)));

            foreach (AppIdentity app in existingApps)
            {
                Assert.False(applicationPool.HasApplication(app));
            }

            foreach (AppIdentity app in newApps)
            {
                Assert.True(applicationPool.HasApplication(app));
            }
        }
    }
}
