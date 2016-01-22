using System.Diagnostics;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Storage.Config;
using Etg.Yams.Utils;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Storage
{
    public class YamsRepository : IYamsRepository
    {
        private readonly CloudBlobContainer _blobContainer;

        public YamsRepository(CloudBlobContainer blobContainer)
        {
            _blobContainer = blobContainer;
        }

        public async Task DeleteApplicationBinaries(AppIdentity appIdentity)
        {
            CloudBlobDirectory blobDirectory = GetBlobDirectory(appIdentity);
            if (!await blobDirectory.ExistsAsync())
            {
                throw new BinariesNotFoundException(
                    $"Cannot delete binaries for application {appIdentity} because they were not found");
            }
            await blobDirectory.DeleteAsync();
        }

        public async Task<DeploymentConfig> FetchDeploymentConfig()
        {
            var blob = _blobContainer.GetBlockBlobReference(Constants.DeploymentConfigFileName);
            if (!await blob.ExistsAsync())
            {
                Trace.TraceInformation("The DeploymentConfig.json file was not found in the Yams repository");
                return new DeploymentConfig();
            }

            string data = await blob.DownloadTextAsync();
            return new DeploymentConfig(data);
        }

        public Task<bool> HasApplicationBinaries(AppIdentity appIdentity)
        {
            return GetBlobDirectory(appIdentity).ExistsAsync();
        }

        public async Task DownloadApplicationBinaries(AppIdentity appIdentity, string localPath, FileMode fileMode)
        {
            bool exists = !FileUtils.DirectoryDoesntExistOrEmpty(localPath);
            if (exists)
            {
                if (fileMode == FileMode.DoNothingIfBinariesExist)
                {
                    return;
                }
                if (fileMode == FileMode.FailIfBinariesExist)
                {
                    throw new DuplicateBinariesException(
                        $"Cannot download the binaries because the destination directory {localPath} contains files");
                }
            }
            CloudBlobDirectory blobDirectory = GetBlobDirectory(appIdentity);
            if (!await blobDirectory.ExistsAsync())
            {
                throw new BinariesNotFoundException("The binaries were not found in the Yams repository");
            }
            await BlobUtils.DownloadBlobDirectory(blobDirectory, localPath);
        }

        public Task PublishDeploymentConfig(DeploymentConfig deploymentConfig)
        {
            CloudBlockBlob blob = _blobContainer.GetBlockBlobReference(Constants.DeploymentConfigFileName);
            return blob.UploadTextAsync(deploymentConfig.RawData());
        }

        public async Task UploadApplicationBinaries(AppIdentity appIdentity, string localPath, FileMode fileMode)
        {
            if (FileUtils.DirectoryDoesntExistOrEmpty(localPath))
            {
                throw new BinariesNotFoundException(
                    $"Binaries were not be uploaded because they were not found at the given path {localPath}");
            }

            if (fileMode == FileMode.OverwriteExistingBinaries)
            {
                await DeleteApplicationBinaries(appIdentity);
            }
            else
            {
                bool exists = await HasApplicationBinaries(appIdentity);

                if (exists)
                {
                    if (fileMode == FileMode.DoNothingIfBinariesExist)
                    {
                        return;
                    }

                    if (fileMode == FileMode.FailIfBinariesExist)
                    {
                        throw new DuplicateBinariesException(
                            $"Cannot override binaries when flag {FileMode.FailIfBinariesExist} is used");
                    }
                }
            }

            // at this point we know that it is either OverwriteExistingBinaries mode or the binaries don't exist
            await BlobUtils.UploadDirectory(localPath, _blobContainer, GetBlobDirectoryRelPath(appIdentity));
        }

        private CloudBlobDirectory GetBlobDirectory(AppIdentity appIdentity)
        {
            string relPath = GetBlobDirectoryRelPath(appIdentity);
            return _blobContainer.GetDirectoryReference(relPath);
        }

        private string GetBlobDirectoryRelPath(AppIdentity appIdentity)
        {
            return appIdentity.Id + "/" + appIdentity.Version;
        }
    }
}