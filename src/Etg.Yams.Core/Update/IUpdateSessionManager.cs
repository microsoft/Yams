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
        /// Starts an update session for the current node. If any other node in a different update domain is being updated,
        /// no session will be started and false will be returned.
        /// </summary>
        /// <returns>True if the session started successfully, false otherwise</returns>
        Task<bool> TryStartUpdateSession();

        /// <summary>
        /// Ends the update session for the current node.
        /// </summary>
        Task EndUpdateSession();
    }
}