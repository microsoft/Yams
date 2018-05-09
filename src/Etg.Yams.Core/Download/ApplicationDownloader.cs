using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Storage;
using Etg.Yams.Utils;

namespace Etg.Yams.Download
{
    public class ApplicationDownloader : IApplicationDownloader
    {
        private readonly string _applicationRootPath;
        private readonly IDeploymentRepository _deploymentRepository;

        /// <summary>
        /// Downloads application from the remote directory to the local file system.
        /// </summary>
        /// <param name="applicationRootPath">The target path where the applications will be downloaded</param>
        /// <param name="deploymentRepository"></param>
        public ApplicationDownloader(string applicationRootPath, IDeploymentRepository deploymentRepository)
        {
            _applicationRootPath = applicationRootPath;
            _deploymentRepository = deploymentRepository;
        }

        public async Task DownloadApplication(AppIdentity appIdentity)
        {
            Trace.TraceInformation($"Downloading application {appIdentity}");
            try
            {
                string destPath = Path.Combine(_applicationRootPath,
                    ApplicationUtils.GetApplicationRelativePath(appIdentity));
                await
                    _deploymentRepository.DownloadApplicationBinaries(appIdentity, destPath,
                        ConflictResolutionMode.OverwriteExistingBinaries);
                Trace.TraceInformation($"Application {appIdentity} downloaded");
            }
            catch (BinariesNotFoundException)
            {
                Trace.TraceError(
                    $"{appIdentity} could not be downloaded because it was not found in the Yams repository");
            }
        }
    }
}