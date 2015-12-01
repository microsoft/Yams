using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Install;
using Etg.Yams.Test.stubs;
using Etg.Yams.Update;
using Etg.Yams.Update.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Etg.Yams.Test.Install
{
    [TestClass]
    public class ApplicationInstallerTest
    {
        private ApplicationInstaller _applicationInstaller;
        private ApplicationPoolStub _applicationPool;
        private string _applicationsRoot;

        [TestInitialize]
        public void Initialize()
        {
            _applicationsRoot = Path.Combine(Directory.GetCurrentDirectory(), "ApplicationInstallerTest");
        }

        [TestMethod]
        public async Task TestInstallApplication()
        {
            _applicationPool = new ApplicationPoolStub();
            IApplicationFactory applicationFactory = new ApplicationFactoryStub();
            _applicationInstaller = new ApplicationInstaller(_applicationsRoot, null, applicationFactory, _applicationPool);

            AppIdentity appIdentity = new AppIdentity("test.app", new Version(1,0,0));
            await _applicationInstaller.Install(appIdentity);

            Assert.IsTrue(_applicationPool.HasApplicationBeenAdded(appIdentity));
        }

        [TestMethod]
        public async Task TestRemoveApplication()
        {
            _applicationPool = new ApplicationPoolStub();
            IApplicationFactory applicationFactory = new ApplicationFactoryStub();
            _applicationInstaller = new ApplicationInstaller(_applicationsRoot, null, applicationFactory, _applicationPool);

            AppIdentity appIdentity = new AppIdentity("test.app", new Version(1, 0, 0));
            _applicationInstaller.Install(appIdentity).Wait();

            // make sure the app directory exists because uninstall will try to delete it
            string appPath = Path.Combine(_applicationsRoot, "test.app", "1.0.0");
            if (!Directory.Exists(appPath))
            {
                Directory.CreateDirectory(appPath);
            }
            await _applicationInstaller.UnInstall(appIdentity);

            Assert.IsFalse(_applicationPool.HasApplication(appIdentity));
            Assert.IsFalse(Directory.Exists(appPath));
        }

        [TestMethod]
        public async Task TestUpdateApplication()
        {
            string updateSessionId = null;
            IUpdateSessionManager updateSessionManager = new StubIUpdateSessionManager()
            {
                TryStartUpdateSessionString = id =>
                {
                    updateSessionId = id;
                    return Task.FromResult(true);
                },
                EndUpdateSessionString = id => Task.FromResult(true)
            };

            IApplicationPool applicationPool = new ApplicationPoolStub();
            IApplicationInstaller applicationInstaller = new ApplicationInstaller(_applicationsRoot, updateSessionManager, new ApplicationFactoryStub(), applicationPool);

            const string appId = "test.app";
            AppIdentity[] existingApps = { new AppIdentity(appId, new Version(1, 0, 0)), new AppIdentity(appId, new Version(1, 0, 1)) };
            AppIdentity[] newApps = { new AppIdentity(appId, new Version(1, 0, 2)), new AppIdentity(appId, new Version(2, 0, 0)) };

            foreach (var existingApp in existingApps)
            {
                string appPath = Path.Combine(_applicationsRoot, existingApp.Id, existingApp.Version.ToString());
                if (!Directory.Exists(appPath))
                {
                    Directory.CreateDirectory(appPath);
                }
                await applicationInstaller.Install(existingApp);
                Assert.IsTrue(applicationPool.HasApplication(existingApp));
            }

            await applicationInstaller.Update(appId, existingApps.Select(app => app.Version), newApps.Select(app => app.Version));

            foreach (AppIdentity app in existingApps)
            {
                Assert.IsFalse(applicationPool.HasApplication(app));
            }

            foreach (AppIdentity app in newApps)
            {
                Assert.IsTrue(applicationPool.HasApplication(app));   
            }
            
            Assert.AreEqual(appId, updateSessionId);
        }

        [TestMethod]
        public async Task TestThatUpdateReturnsIfCannotStartUpdateSession()
        {
            IUpdateSessionManager updateSessionManager = new StubIUpdateSessionManager()
            {
                TryStartUpdateSessionString = id => Task.FromResult(false)
            };

            IApplicationPool applicationPool = new ApplicationPoolStub();
            IApplicationInstaller applicationInstaller = new ApplicationInstaller(_applicationsRoot, updateSessionManager, new ApplicationFactoryStub(), applicationPool);

            AppIdentity existingApp = new AppIdentity("test.app", new Version(1, 0, 0));
            await applicationInstaller.Install(existingApp);

            AppIdentity newApp = new AppIdentity(existingApp.Id, new Version(1, 1, 0));
            await applicationInstaller.Update(existingApp.Id, new[] {existingApp.Version}, new[] {newApp.Version});

            Assert.IsTrue(applicationPool.HasApplication(existingApp));
            Assert.IsFalse(applicationPool.HasApplication(newApp));
        }
    }
}
