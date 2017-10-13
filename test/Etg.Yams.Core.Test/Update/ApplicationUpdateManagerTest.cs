using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Deploy;
using Etg.Yams.Download;
using Etg.Yams.Install;
using Etg.Yams.Storage.Config;
using Etg.Yams.Test.stubs;
using Etg.Yams.Update;
using Semver;
using Xunit;
using Etg.Yams.Storage;
using Etg.Yams.Storage.Status;

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
            IEnumerable<string> clusters = new[] {"clusterId1"};

	        IApplicationDeploymentDirectory applicationDeploymentDirectory = new StubIApplicationDeploymentDirectory()
		        .FetchDeployments(() => Task.FromResult(appsToDeploy.Select(identity => new AppDeploymentConfig(identity, clusters))));

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

            IApplicationInstaller applicationInstaller = new StubIApplicationInstaller()
                .Install(config => 
                {
                    applicationPool.AddApplication(new ApplicationStub(config.AppIdentity, "path"));
                    return Task.FromResult(true);
                })
                .UnInstall(appIdentity =>
                {
                    applicationPool.RemoveApplication(appIdentity);
                    return Task.FromResult(true);
                })
                .Update((applicationsToRemove, applicationsToInstall) => 
                {
                    foreach(var appIdentity in applicationsToRemove)
                    {
                        applicationPool.RemoveApplication(appIdentity);
                    }
                    foreach(var appInstallConfig in applicationsToInstall)
                    {
                        applicationPool.AddApplication(new ApplicationStub(appInstallConfig.AppIdentity, "path"));
                    }
	                return Task.FromResult(true);
                }
            );

            var instanceDeploymentStatus = new InstanceDeploymentStatus();
            IDeploymentStatusWriter deploymentStatusWriterStub = new StubIDeploymentStatusWriter()
                .PublishInstanceDeploymentStatus((clusterId, instanceId, status) =>
                {
                    instanceDeploymentStatus = status;
                    return Task.CompletedTask;
                });

            const string ClusterId = "clusterId";
            const string InstanceId = "instanceId";
            ApplicationUpdateManager applicationUpdateManager = new ApplicationUpdateManager(ClusterId, InstanceId, 
                applicationDeploymentDirectory, applicationPool, applicationDownloader, applicationInstaller,
                deploymentStatusWriterStub);
            await applicationUpdateManager.CheckForUpdates();

            Assert.Equal(3, applicationPool.Applications.Count());
            Assert.True(applicationPool.HasApplication(app1v3));
            Assert.True(applicationPool.HasApplication(app1v4));
            Assert.True(applicationPool.HasApplication(app1v5));

            Assert.Equal(3, instanceDeploymentStatus.Applications.Count());
            VerifyThatDeploymentStatusHasBeenUpdated(instanceDeploymentStatus, app1v3, ClusterId, InstanceId);
            VerifyThatDeploymentStatusHasBeenUpdated(instanceDeploymentStatus, app1v4, ClusterId, InstanceId);
            VerifyThatDeploymentStatusHasBeenUpdated(instanceDeploymentStatus, app1v5, ClusterId, InstanceId);
        }

        private void VerifyThatDeploymentStatusHasBeenUpdated(InstanceDeploymentStatus deploymentStatus, 
            AppIdentity appIdentity, string clusterId, string instanceId)
        {
            var appDeploymentStatus = deploymentStatus.GetAppDeploymentStatus(appIdentity);
            Assert.NotNull(appDeploymentStatus);
            Assert.Equal(appIdentity, appDeploymentStatus.AppIdentity);
            Assert.Equal(clusterId, appDeploymentStatus.ClusterId);
            Assert.Equal(instanceId, appDeploymentStatus.InstanceId);
            Assert.NotNull(appDeploymentStatus.UtcTimeStamp);
        }
    }
}
