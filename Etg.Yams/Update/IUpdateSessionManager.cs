using System.Threading.Tasks;

namespace Etg.Yams.Update
{
    /// <summary>
    /// The update session is used to preserve the notion of "update domains" in Azure. When an application is updated,
    /// it will be deployed on one update domain at a time to minimize down-time. 
    /// </summary>
    public interface IUpdateSessionManager
    {
        /// <summary>
        /// Starts an update session for the given application. If the application is being updated on another instance in a different update domain,
        /// no session will be started and false will be returned.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        Task<bool> TryStartUpdateSession(string applicationId);

        /// <summary>
        /// Ends the update session for the current role instance in the current update domain.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        Task EndUpdateSession(string applicationId);
    }
}