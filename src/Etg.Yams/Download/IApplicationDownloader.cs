using System.Threading.Tasks;
using Etg.Yams.Application;

namespace Etg.Yams.Download
{
    public interface IApplicationDownloader
    {
        /// <summary>
        /// Download the application associated with the given <see cref="AppIdentity"/> to the current Role instance.
        /// </summary>
        /// <param name="appIdentity"></param>
        /// <returns></returns>
        Task DownloadApplication(AppIdentity appIdentity);
    }
}
