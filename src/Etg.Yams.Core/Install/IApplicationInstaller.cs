using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Etg.Yams.Application;

namespace Etg.Yams.Install
{
    /// <summary>
    /// An application installer is used to create, run and register an application.
    /// The application must be downloaded before installation (downloading is NOT the responsibility of this class).
    /// </summary>
    public interface IApplicationInstaller
    {
        /// <summary>
        /// Creates, runs and registers the application
        /// </summary>
        /// <param name="appInstallConfig"></param>
        /// <returns></returns>
        Task Install(AppInstallConfig appInstallConfig);

        /// <summary>
        /// Stops and unregister the application.
        /// </summary>
        /// <param name="appIdentity"></param>
        /// <returns></returns>
        Task UnInstall(AppIdentity appIdentity);

        /// <summary>
        /// Performs an update. All given applications must have the same application id.
        /// The main difference between install/uninstall and update is that update obey the update domain.
        /// In other words, applications are updated in one update domain at a time.
        /// </summary>
        /// <param name="applicationsToRemove"></param>
        /// <param name="applicationsToDeploy"></param>
        /// <returns></returns>
        Task<bool> Update(IEnumerable<AppIdentity> applicationsToRemove, IEnumerable<AppInstallConfig> applicationsToDeploy);
    }
}
