using System.Threading.Tasks;

namespace Etg.Yams.Update
{
    /// <summary>
    /// Scans the remote storage to figure out if applications need to be installed, removed or updated; and performs 
    /// the corresponding work.
    ///  </summary>
    public interface IApplicationUpdateManager
    {
        Task CheckForUpdates();
    }
}
