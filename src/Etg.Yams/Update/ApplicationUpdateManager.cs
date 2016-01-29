using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Deploy;
using Etg.Yams.Download;
using Etg.Yams.Install;
using Etg.Yams.Utils;

namespace Etg.Yams.Update
{
    public class ApplicationUpdateManager : IApplicationUpdateManager
    {
        private readonly string _cloudServiceDeploymentId;
        private readonly IApplicationDeploymentDirectory _applicationDeploymentDirectory;
        private readonly IApplicationPool _applicationPool;
        private readonly IApplicationDownloader _applicationDownloader;
        private readonly IApplicationInstaller _applicationInstaller;

        public ApplicationUpdateManager(
            string cloudServiceDeploymentId,
            IApplicationDeploymentDirectory applicationDeploymentDirectory,
            IApplicationPool applicationPool, 
            IApplicationDownloader applicationDownloader, 
            IApplicationInstaller applicationInstaller)
        {
            _cloudServiceDeploymentId = cloudServiceDeploymentId;
            _applicationDeploymentDirectory = applicationDeploymentDirectory;
            _applicationPool = applicationPool;
            _applicationDownloader = applicationDownloader;
            _applicationInstaller = applicationInstaller;
        }

        public async Task CheckForUpdates()
        {
            try
            {
                Trace.TraceInformation("Checking for updates");

                IEnumerable<AppIdentity> applicationDeployments = await _applicationDeploymentDirectory.FetchDeployments(_cloudServiceDeploymentId);
                IEnumerable<AppIdentity> runningApplications = _applicationPool.Applications.Select(app => app.Identity);

                IEnumerable<AppIdentity> applicationsToRemove = FindApplicationsToRemove(runningApplications, applicationDeployments);
                IEnumerable<AppIdentity> applicationsToDeploy = FindApplicationsToDeploy(runningApplications, applicationDeployments);

                // download applications first
                await DownloadApplications(applicationsToDeploy);

                IEnumerable<string> allAppsIds = new HashSet<string>(applicationsToRemove.Union(applicationsToDeploy).Select(identity => identity.Id));

                var tasks = new List<Task>();
                foreach (string appId in allAppsIds)
                {
                    IEnumerable<AppIdentity> appRemovals = applicationsToRemove.Where(identity => identity.Id.Equals(appId));
                    IEnumerable<AppIdentity> appDeployments = applicationsToDeploy.Where(identity => identity.Id.Equals(appId));

                    // if an application has a removal(s) and deployment(s), we consider it an update
                    if (appRemovals.Any() && appDeployments.Any())
                    {
                        tasks.Add(_applicationInstaller.Update(appId, appRemovals.Select(i => i.Version), appDeployments.Select(i => i.Version)));
                    }
                    else
                    {
                        foreach (AppIdentity appDeployment in appDeployments)
                        {
                            tasks.Add(_applicationInstaller.Install(appDeployment));
                        }
                        foreach (AppIdentity appRemoval in appRemovals)
                        {
                            tasks.Add(_applicationInstaller.UnInstall(appRemoval));
                        }
                    }
                }
                await Task.WhenAll(tasks);
            }
            catch (AggregateException e)
            {
                TraceUtils.TraceAllErrors("Failures occured during updates", e);
            }
            catch (Exception e)
            {
                Trace.TraceError("Failed to perform update; Exception was: {0}", e);
            }
        }

        private Task DownloadApplications(IEnumerable<AppIdentity> appDeployments)
        {
            List<Task> tasks = new List<Task>();
            foreach (AppIdentity appIdentity in appDeployments)
            {
                tasks.Add(_applicationDownloader.DownloadApplication(appIdentity));
            }
            return Task.WhenAll(tasks);
        }

        private IEnumerable<AppIdentity> FindApplicationsToRemove(IEnumerable<AppIdentity> runningApplications, IEnumerable<AppIdentity> applicationDeployments)
        {
            ISet<AppIdentity> deploymentIds = new HashSet<AppIdentity>(applicationDeployments);
            return runningApplications.Where(identity => !deploymentIds.Contains(identity));
        }

        private IEnumerable<AppIdentity> FindApplicationsToDeploy(IEnumerable<AppIdentity> runningApplications, IEnumerable<AppIdentity> applicationDeployments)
        {
            ISet<AppIdentity> runningAppsIds = new HashSet<AppIdentity>(runningApplications);
            return applicationDeployments.Where(identity => !runningAppsIds.Contains(identity));
        } 
    }
}
