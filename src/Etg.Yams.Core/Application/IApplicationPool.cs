using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Etg.Yams.Application
{
    public interface IApplicationPool : IDisposable
    {
        /// <summary>
        /// Will start the given application and add it to the pool.
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        Task AddApplication(IApplication application);

        /// <summary>
        /// Will stop the application and remove it from the pool.
        /// </summary>
        /// <param name="appIdentity"></param>
        /// <returns></returns>
        Task RemoveApplication(AppIdentity appIdentity);

        bool HasApplication(AppIdentity appIdentity);

        /// <summary>
        /// Returns null if the application is not in the pool.
        /// </summary>
        /// <param name="appIdentity"></param>
        /// <returns></returns>
        IApplication GetApplication(AppIdentity appIdentity);

        /// <summary>
        /// All running applications.
        /// </summary>
        IEnumerable<IApplication> Applications { get; }

        /// <summary>
        /// Stops all applications and removes them from the pool.
        /// </summary>
        /// <returns></returns>
        Task Shutdown();
    }
}