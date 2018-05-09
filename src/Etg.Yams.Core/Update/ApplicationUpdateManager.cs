using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Deploy;
using Etg.Yams.Download;
using Etg.Yams.Install;
using Etg.Yams.Storage.Config;
using Etg.Yams.Utils;
using Etg.Yams.Storage;
using Etg.Yams.Storage.Status;

namespace Etg.Yams.Update
{
    public class ApplicationUpdateManager : IApplicationUpdateManager
    {
        private readonly string _clusterId;
        private readonly string _instanceId;
        private readonly IApplicationDeploymentDirectory _applicationDeploymentDirectory;
        private readonly IApplicationPool _applicationPool;
        private readonly IApplicationDownloader _applicationDownloader;
        private readonly IApplicationInstaller _applicationInstaller;
        private readonly IDeploymentStatusWriter _deploymentStatusWriter;
        private readonly IUpdateSessionManager _updateSessionManager;

        public ApplicationUpdateManager(
            string clusterId,
            string instanceId,
            IApplicationDeploymentDirectory applicationDeploymentDirectory,
            IApplicationPool applicationPool, 
            IApplicationDownloader applicationDownloader, 
            IApplicationInstaller applicationInstaller,
            IDeploymentStatusWriter deploymentStatusWriter,
            IUpdateSessionManager updateSessionManager)
        {
            _clusterId = clusterId;
            this._instanceId = instanceId;
            _applicationDeploymentDirectory = applicationDeploymentDirectory;
            _applicationPool = applicationPool;
            _applicationDownloader = applicationDownloader;
            _applicationInstaller = applicationInstaller;
            _deploymentStatusWriter = deploymentStatusWriter;
            _updateSessionManager = updateSessionManager;
        }

        public async Task CheckForUpdates()
        {
            try
            {
                Trace.TraceInformation("Checking for updates");

                IEnumerable<AppDeploymentConfig> applicationDeployments = await _applicationDeploymentDirectory.FetchDeployments();
                IEnumerable<AppIdentity> runningApplications = _applicationPool.Applications.Select(app => app.Identity).ToList();

                IEnumerable<AppIdentity> applicationsToRemove = FindApplicationsToRemove(runningApplications, applicationDeployments);
                IEnumerable<AppDeploymentConfig> applicationsToDeploy = FindApplicationsToDeploy(runningApplications, applicationDeployments);

                if (!applicationsToDeploy.Any() && !applicationsToRemove.Any())
                {
                    return;
                }

                // download applications first
                await DownloadApplications(applicationsToDeploy);

                var allAppsIds = new HashSet<string>(applicationsToRemove.Select(identity => identity.Id));
                allAppsIds.UnionWith(applicationsToDeploy.Select(config => config.AppIdentity.Id));

                if (!await _updateSessionManager.TryStartUpdateSession())
                {
                    Trace.TraceInformation("Couldn't start update session");
                    return;
                }

                var tasks = new List<Task>();
                
                foreach (string appId in allAppsIds)
                {
                    IEnumerable<AppIdentity> appRemovals = applicationsToRemove.Where(identity => identity.Id.Equals(appId));
                    IEnumerable<AppDeploymentConfig> appDeployments = applicationsToDeploy.Where(config => config.AppIdentity.Id.Equals(appId));

                    // if an application has a removal(s) and deployment(s), we consider it an update
                    if (appRemovals.Any() && appDeployments.Any())
                    {
                        tasks.Add(_applicationInstaller.Update(appRemovals, appDeployments));
                    }
                    else
                    {
                        foreach (AppDeploymentConfig appDeploymentConfig in appDeployments)
                        {
                            tasks.Add(_applicationInstaller.Install(appDeploymentConfig));
                        }
                        foreach (AppIdentity appRemoval in appRemovals)
                        {
                            tasks.Add(_applicationInstaller.UnInstall(appRemoval));
                        }
                    }
                }
                await Task.WhenAll(tasks);

                // We will only end the update session if the update was successful. Otherwise, the update session will stay open and will prevent
                // the deployment from moving to the next update domain.
                await _updateSessionManager.EndUpdateSession(); 
            }
            catch (AggregateException e)
            {
                TraceUtils.TraceAllErrors("Failures occured during updates", e);
            }
            catch (Exception e)
            {
                Trace.TraceError("Failed to perform update; Exception: {0}", e);
            }
            finally
            {
                await UpdateDeploymentStatus();
            }
        }

        private async Task UpdateDeploymentStatus()
        {
            try
            {
                DateTime utcNow = DateTime.UtcNow;
                var instanceDeploymentStatus = new InstanceDeploymentStatus();
                foreach (IApplication app in _applicationPool.Applications)
                {
                    var appDeploymentStatus = new AppDeploymentStatus(app.Identity, _clusterId, _instanceId, utcNow);
                    instanceDeploymentStatus.SetAppDeploymentStatus(appDeploymentStatus);
                }
                await _deploymentStatusWriter.PublishInstanceDeploymentStatus(_clusterId, _instanceId, instanceDeploymentStatus);
                Trace.TraceInformation("DeploymentStatus updated");
            }
            catch (Exception e)
            {
                Trace.TraceError("Failed to update the deployment status; Exception: {0}", e);
            }
        }

        private async Task DownloadApplications(IEnumerable<AppDeploymentConfig> appDeployments)
        {
            Trace.TraceInformation("Downloading applications");
            List<Task> tasks = new List<Task>();
            foreach (AppDeploymentConfig appDeploymentConfig in appDeployments)
            {
                tasks.Add(_applicationDownloader.DownloadApplication(appDeploymentConfig.AppIdentity));
            }
            await Task.WhenAll(tasks);
            Trace.TraceInformation("Download applications complete");
        }

        private IEnumerable<AppIdentity> FindApplicationsToRemove(IEnumerable<AppIdentity> runningApplications, IEnumerable<AppDeploymentConfig> applicationDeployments)
        {
            ISet<AppIdentity> appIdentities = new HashSet<AppIdentity>(applicationDeployments.Select(config => config.AppIdentity));
            return runningApplications.Where(identity => !appIdentities.Contains(identity));
        }

        private IEnumerable<AppDeploymentConfig> FindApplicationsToDeploy(IEnumerable<AppIdentity> runningApplications, IEnumerable<AppDeploymentConfig> applicationDeployments)
        {
            ISet<AppIdentity> runningAppsIds = new HashSet<AppIdentity>(runningApplications);
            return applicationDeployments.Where(config => !runningAppsIds.Contains(config.AppIdentity));
        } 
    }
}
