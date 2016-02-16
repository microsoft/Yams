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
        /// <param name="appIdentity"></param>
        /// <returns></returns>
        Task Install(AppIdentity appIdentity);

        /// <summary>
        /// Stops and unregister the application.
        /// </summary>
        /// <param name="appIdentity"></param>
        /// <returns></returns>
        Task UnInstall(AppIdentity appIdentity);

        /// <summary>
        /// Updated the given application.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="versionsToRemove"></param>
        /// <param name="versionsToDeploy"></param>
        /// <returns></returns>
        Task<bool> Update(string appId, IEnumerable<Version> versionsToRemove, IEnumerable<Version> versionsToDeploy);
    }
}
