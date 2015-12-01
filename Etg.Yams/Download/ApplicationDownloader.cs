using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.IO;
using Etg.Yams.Utils;

namespace Etg.Yams.Download
{
    public class ApplicationDownloader : IApplicationDownloader
    {
        private readonly string _applicationRootPath;
        private readonly IRemoteDirectory _deploymentsRootDirectory;

        /// <summary>
        /// Downloads application from the remote directory to the local file system.
        /// </summary>
        /// <param name="applicationRootPath">The target path where the applications will be downloaded</param>
        /// <param name="deploymentsRootDirectory">The remote directory from where the applications will be downloaded</param>
        public ApplicationDownloader(string applicationRootPath, IRemoteDirectory deploymentsRootDirectory)
        {
            _applicationRootPath = applicationRootPath;
            _deploymentsRootDirectory = deploymentsRootDirectory;
        }

        public async Task DownloadApplication(AppIdentity appIdentity)
        {
            IRemoteDirectory appDeploymentDir =
                await _deploymentsRootDirectory.GetDirectory(appIdentity.Id);
            if (await appDeploymentDir.Exists())
            {
                IRemoteDirectory versionDir = await appDeploymentDir.GetDirectory(appIdentity.Version.ToString());
                if (await versionDir.Exists())
                {
                    await
                        versionDir.Download(Path.Combine(_applicationRootPath,
                            ApplicationUtils.GetApplicationRelativePath(appIdentity)));
                }
                return;
            }
            Trace.TraceError("{0} could not be downloaded because it was not found in the blob storage", appIdentity);
        }
    }
}