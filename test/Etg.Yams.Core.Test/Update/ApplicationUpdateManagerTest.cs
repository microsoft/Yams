using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Deploy;
using Etg.Yams.Download;
using Etg.Yams.Storage.Config;
using Etg.Yams.Test.stubs;
using Etg.Yams.Update;
using Semver;
using Xunit;
using Etg.Yams.Storage;
using Etg.Yams.Storage.Status;
using Etg.Yams.Install;
using System;

namespace Etg.Yams.Test.Update
{
    public class ApplicationUpdateManagerTest
    {
        private readonly AppIdentity app1v1;
        private readonly AppIdentity app1v2;
        private readonly AppIdentity app1v3;
        private readonly AppIdentity app1v4;
        private readonly AppIdentity app1v5;
        private readonly List<AppIdentity> downloadedApps;
        private readonly StubIApplicationDownloader applicationDownloader;
        private readonly ApplicationPoolStub applicationPool;
        private readonly ApplicationInstallerStub applicationInstaller;

        private InstanceDeploymentStatus instanceDeploymentStatus;
        private readonly StubIDeploymentStatusWriter deploymentStatusWriterStub;

        public ApplicationUpdateManagerTest()
        {
            string id1 = "appId1";
            var v1 = SemVersion.Parse("1.0.0");
            var v2 = SemVersion.Parse("2.0.0");
            var v3 = SemVersion.Parse("3.0.0");
            var v4 = SemVersion.Parse("4.0.0");
            var v5 = SemVersion.Parse("5.0.0");

            app1v1 = new AppIdentity(id1, v1);
            app1v2 = new AppIdentity(id1, v2);
            app1v3 = new AppIdentity(id1, v3);
            app1v4 = new AppIdentity(id1, v4);
            app1v5 = new AppIdentity(id1, v5);

            downloadedApps = new List<AppIdentity>();
            applicationDownloader = new StubIApplicationDownloader()
                .DownloadApplication(appIdentity =>
                {
                    downloadedApps.Add(appIdentity);
                    return Task.FromResult(true);
                }
            );

            applicationPool = new ApplicationPoolStub();
            applicationInstaller = new ApplicationInstallerStub(applicationPool, "path");

            instanceDeploymentStatus = new InstanceDeploymentStatus();
            deploymentStatusWriterStub = new StubIDeploymentStatusWriter()
                .PublishInstanceDeploymentStatus((clusterId, instanceId, status) =>
                {
                    instanceDeploymentStatus = status;
                    return Task.CompletedTask;
                });
        }

        [Fact]
        public async Task TestMultipleUpdates()
        {
            IEnumerable<AppIdentity> appsToDeploy = new[] {app1v3, app1v4, app1v5};
            IEnumerable<string> clusters = new[] {"clusterId1"};

	        IApplicationDeploymentDirectory applicationDeploymentDirectory = new StubIApplicationDeploymentDirectory()
		        .FetchDeployments(() => Task.FromResult(appsToDeploy.Select(identity => new AppDeploymentConfig(identity, clusters))));

            string path = Path.GetTempPath();
            await applicationPool.AddApplication(new ApplicationStub(app1v1, path));
            await applicationPool.AddApplication(new ApplicationStub(app1v2, path));
            await applicationPool.AddApplication(new ApplicationStub(app1v3, path));

            IUpdateSessionManager updateSessionManagerStub = new StubIUpdateSessionManager()
                .TryStartUpdateSession(() => Task.FromResult(true))
                .EndUpdateSession(() => Task.CompletedTask);

            const string ClusterId = "clusterId";
            const string InstanceId = "instanceId";
            ApplicationUpdateManager applicationUpdateManager = new ApplicationUpdateManager(ClusterId, InstanceId, 
                applicationDeploymentDirectory, applicationPool, applicationDownloader, applicationInstaller,
                deploymentStatusWriterStub, updateSessionManagerStub);
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

        [Fact]
        public async Task TestThatUpdateDoesNothingIfCannotStartUpdateSession()
        {
            IEnumerable<AppIdentity> appsToDeploy = new[] { app1v2 };
            IEnumerable<string> clusters = new[] { "clusterId1" };

            IApplicationPool applicationPool = new ApplicationPoolStub();
            string path = Path.GetTempPath();
            await applicationPool.AddApplication(new ApplicationStub(app1v1, path));

            IUpdateSessionManager updateSessionManagerStub = new StubIUpdateSessionManager()
                .TryStartUpdateSession(() => Task.FromResult(false));

            IApplicationDeploymentDirectory applicationDeploymentDirectory = new StubIApplicationDeploymentDirectory()
                .FetchDeployments(() => Task.FromResult(appsToDeploy.Select(identity => new AppDeploymentConfig(identity, clusters))));

            const string ClusterId = "clusterId";
            const string InstanceId = "instanceId";
            ApplicationUpdateManager applicationUpdateManager = new ApplicationUpdateManager(ClusterId, InstanceId,
                applicationDeploymentDirectory, applicationPool, applicationDownloader, applicationInstaller,
                deploymentStatusWriterStub, updateSessionManagerStub);
            await applicationUpdateManager.CheckForUpdates();

            Assert.Equal(1, applicationPool.Applications.Count());
            Assert.True(applicationPool.HasApplication(app1v1));
        }


        [Fact]
        public async Task TestThatUpdateSessionIsEndedFollowingASuccessfulUpdate()
        {
            IEnumerable<AppIdentity> appsToDeploy = new[] { app1v2 };
            IEnumerable<string> clusters = new[] { "clusterId1" };

            IApplicationPool applicationPool = new ApplicationPoolStub();
            string path = Path.GetTempPath();
            await applicationPool.AddApplication(new ApplicationStub(app1v1, path));

            bool updateSessionEnded = false;
            IUpdateSessionManager updateSessionManagerStub = new StubIUpdateSessionManager()
                .TryStartUpdateSession(() => Task.FromResult(true))
                .EndUpdateSession(() =>
                {
                    updateSessionEnded = true;
                    return Task.CompletedTask;
                });

            IApplicationDeploymentDirectory applicationDeploymentDirectory = new StubIApplicationDeploymentDirectory()
                .FetchDeployments(() => Task.FromResult(appsToDeploy.Select(identity => new AppDeploymentConfig(identity, clusters))));

            var applicationInstallerStub = new StubIApplicationInstaller()
                .Install((config) => throw new Exception("Failed to install application"));
            const string ClusterId = "clusterId";
            const string InstanceId = "instanceId";
            ApplicationUpdateManager applicationUpdateManager = new ApplicationUpdateManager(ClusterId, InstanceId,
                applicationDeploymentDirectory, applicationPool, applicationDownloader, applicationInstallerStub,
                deploymentStatusWriterStub, updateSessionManagerStub);
            await applicationUpdateManager.CheckForUpdates();

            Assert.False(updateSessionEnded);
        }


        [Fact]
        public async Task TestThatUpdateSessionIsNotEndedWhenUpdateFails()
        {
            IEnumerable<AppIdentity> appsToDeploy = new[] { app1v2 };
            IEnumerable<string> clusters = new[] { "clusterId1" };

            IApplicationPool applicationPool = new ApplicationPoolStub();
            string path = Path.GetTempPath();
            await applicationPool.AddApplication(new ApplicationStub(app1v1, path));

            bool updateSessionEnded = false;
            IUpdateSessionManager updateSessionManagerStub = new StubIUpdateSessionManager()
                .TryStartUpdateSession(() => Task.FromResult(true))
                .EndUpdateSession(() =>
                {
                    updateSessionEnded = true;
                    return Task.CompletedTask;
                });

            IApplicationDeploymentDirectory applicationDeploymentDirectory = new StubIApplicationDeploymentDirectory()
                .FetchDeployments(() => Task.FromResult(appsToDeploy.Select(identity => new AppDeploymentConfig(identity, clusters))));

            const string ClusterId = "clusterId";
            const string InstanceId = "instanceId";
            ApplicationUpdateManager applicationUpdateManager = new ApplicationUpdateManager(ClusterId, InstanceId,
                applicationDeploymentDirectory, applicationPool, applicationDownloader, applicationInstaller,
                deploymentStatusWriterStub, updateSessionManagerStub);
            await applicationUpdateManager.CheckForUpdates();

            Assert.True(updateSessionEnded);
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
