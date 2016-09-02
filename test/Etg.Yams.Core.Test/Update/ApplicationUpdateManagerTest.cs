using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Deploy;
using Etg.Yams.Download;
using Etg.Yams.Install;
using Etg.Yams.Test.stubs;
using Etg.Yams.Update;
using Semver;
using Xunit;

namespace Etg.Yams.Test.Update
{
    public class ApplicationUpdateManagerTest
    {
        [Fact]
        public async Task TestMultipleUpdates()
        {
            string id1 = "appId1";
            var v1 = SemVersion.Parse("1.0.0");
            var v2 = SemVersion.Parse("2.0.0");
            var v3 = SemVersion.Parse("3.0.0");
            var v4 = SemVersion.Parse("4.0.0");
            var v5 = SemVersion.Parse("5.0.0");

            AppIdentity app1v1 = new AppIdentity(id1, v1);
            AppIdentity app1v2 = new AppIdentity(id1, v2);
            AppIdentity app1v3 = new AppIdentity(id1, v3);
            AppIdentity app1v4 = new AppIdentity(id1, v4);
            AppIdentity app1v5 = new AppIdentity(id1, v5);

            IEnumerable<AppIdentity> appsToDeploy = new[] {app1v3, app1v4, app1v5};

	        IApplicationDeploymentDirectory applicationDeploymentDirectory = new StubIApplicationDeploymentDirectory()
		        .FetchDeployments(deploymentId => Task.FromResult(appsToDeploy));

            IApplicationPool applicationPool = new ApplicationPoolStub();
            string path = Path.GetTempPath();
            await applicationPool.AddApplication(new ApplicationStub(app1v1, path));
            await applicationPool.AddApplication(new ApplicationStub(app1v2, path));
            await applicationPool.AddApplication(new ApplicationStub(app1v3, path));

            var downloadedApps = new List<AppIdentity>();
            IApplicationDownloader applicationDownloader = new StubIApplicationDownloader()
                .DownloadApplication(appIdentity =>
                {
                    downloadedApps.Add(appIdentity);
                    return Task.FromResult(true);
                }
            );

            var installedApps = new List<AppIdentity>();
            var uninstalledApps = new List<AppIdentity>();
            string updatedAppId = null;
            IEnumerable<SemVersion> versionsRemoved = null;
            IEnumerable<SemVersion> versionsAdded = null;
            IApplicationInstaller applicationInstaller = new StubIApplicationInstaller()
                .Install(appIdentity =>
                {
                    installedApps.Add(appIdentity);
                    return Task.FromResult(true);
                })
                .UnInstall(appIdentity =>
                {
                    uninstalledApps.Add(appIdentity);
                    return Task.FromResult(true);
                })
                .Update((appId, versionsToRemove, versionToDeploy) =>
                {
	                updatedAppId = appId;
	                versionsRemoved = versionsToRemove;
	                versionsAdded = versionToDeploy;
	                return Task.FromResult(true);
                }
            );

            ApplicationUpdateManager applicationUpdateManager = new ApplicationUpdateManager("deploymentId", applicationDeploymentDirectory, applicationPool, applicationDownloader, applicationInstaller);
            await applicationUpdateManager.CheckForUpdates();

            Assert.Equal(2, downloadedApps.Count);
            Assert.True(downloadedApps.Contains(app1v4));
            Assert.True(downloadedApps.Contains(app1v5));

            Assert.False(installedApps.Any());
            Assert.False(uninstalledApps.Any());

            Assert.Equal(id1, updatedAppId);
            Assert.Equal(new []{ v1, v2 }, versionsRemoved.ToList());
            Assert.Equal(new[] { v4, v5 }, versionsAdded.ToList());
        }
    }
}
