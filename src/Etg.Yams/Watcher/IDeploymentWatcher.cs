using System.Threading.Tasks;

namespace Etg.Yams.Watcher
{
    /// <summary>
    /// Periodically check the remote storage for applications updates.
    /// </summary>
    public interface IDeploymentWatcher
    {
        Task Start();

        Task Stop();
    }
}
