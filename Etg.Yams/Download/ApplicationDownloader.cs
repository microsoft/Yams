using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Storage;
using Etg.Yams.Utils;
using FileMode = Etg.Yams.Storage.FileMode;

namespace Etg.Yams.Download
{
    public class ApplicationDownloader : IApplicationDownloader
    {
        private readonly string _applicationRootPath;
        private readonly IYamsRepository _yamsRepository;

        /// <summary>
        /// Downloads application from the remote directory to the local file system.
        /// </summary>
        /// <param name="applicationRootPath">The target path where the applications will be downloaded</param>
        /// <param name="yamsRepository"></param>
        public ApplicationDownloader(string applicationRootPath, IYamsRepository yamsRepository)
        {
            _applicationRootPath = applicationRootPath;
            _yamsRepository = yamsRepository;
        }

        public async Task DownloadApplication(AppIdentity appIdentity)
        {
            try
            {
                string destPath = Path.Combine(_applicationRootPath,
                    ApplicationUtils.GetApplicationRelativePath(appIdentity));
                await
                    _yamsRepository.DownloadApplicationBinaries(appIdentity, destPath,
                        FileMode.OverwriteExistingBinaries);
            }
            catch (BinariesNotFoundException)
            {
                Trace.TraceError(
                    $"{appIdentity} could not be downloaded because it was not found in the Yams repository");
            }
        }
    }
}