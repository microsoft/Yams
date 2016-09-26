﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Storage.Config;
using Etg.Yams.Update;
using Etg.Yams.Utils;
using Semver;

namespace Etg.Yams.Install
{
    public class ApplicationInstaller : IApplicationInstaller
    {
        private readonly string _applicationsRootPath;
        private readonly IUpdateSessionManager _updateSessionManager;
        private readonly IApplicationFactory _applicationFactory;
        private readonly IApplicationPool _applicationPool;

        public ApplicationInstaller(string applicationsRootPath, IUpdateSessionManager updateSessionManager, IApplicationFactory applicationFactory,
            IApplicationPool applicationPool)
        {
            _applicationsRootPath = applicationsRootPath;
            _updateSessionManager = updateSessionManager;
            _applicationFactory = applicationFactory;
            _applicationPool = applicationPool;
        }

        public async Task Install(AppInstallConfig appInstallConfig)
        {
            IApplication application =
                await _applicationFactory.CreateApplication(appInstallConfig, GetApplicationAbsolutePath(appInstallConfig.AppIdentity));
            await _applicationPool.AddApplication(application);
        }

        public async Task UnInstall(AppIdentity appIdentity)
        {
            await _applicationPool.RemoveApplication(appIdentity);
            await FileUtils.DeleteDirectoryIfAny(GetApplicationAbsolutePath(appIdentity), recursive:true);
        }

        public async Task<bool> Update(IEnumerable<AppIdentity> applicationsToRemove, IEnumerable<AppInstallConfig> applicationsToDeploy)
        {
            if (!applicationsToDeploy.Any() || !applicationsToRemove.Any())
            {
                throw new ArgumentException("An update must at least involve an application to remove and an application to deploy");
            }

            string appId = applicationsToDeploy.First().AppIdentity.Id;

            bool failed = false;
            try
            {
                if (!await _updateSessionManager.TryStartUpdateSession(appId))
                {
                    Trace.TraceInformation("Couldn't start update session for app {0}", appId);
                    return false;
                }

                await UnInstallApplications(applicationsToRemove);
                await InstallApplications(applicationsToDeploy);
            }
            catch (AggregateException e)
            {
                failed = true;
                TraceUtils.TraceAllErrors("Failed to update applications", e);
            }
            catch (Exception e)
            {
                failed = true;
                Trace.TraceError("Failed to update applications", e);
            }

            await _updateSessionManager.EndUpdateSession(appId);

            if(failed)
            {
                return false;
            }
            return true;
        }

        private async Task UnInstallApplications(IEnumerable<AppIdentity> applicationsToRemove)
        {
            var tasks = new List<Task>();
            foreach (AppIdentity appIdentity in applicationsToRemove)
            {
                tasks.Add(UnInstall(appIdentity));
            }
            await Task.WhenAll(tasks);
        }

        private async Task InstallApplications(IEnumerable<AppInstallConfig> applications)
        {
            var tasks = new List<Task>();
            foreach (AppInstallConfig appInstallConfig in applications)
            {
                tasks.Add(Install(appInstallConfig));
            }
            await Task.WhenAll(tasks);
        }

        private string GetApplicationAbsolutePath(AppIdentity appIdentity)
        {
            return Path.Combine(_applicationsRootPath, ApplicationUtils.GetApplicationRelativePath(appIdentity));
        }
    }
}