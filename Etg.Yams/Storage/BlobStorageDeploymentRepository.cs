using System.Diagnostics;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Storage.Config;
using Etg.Yams.Utils;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Storage
{
    public class BlobStorageDeploymentRepository : IDeploymentRepository
    {
        private readonly CloudBlobContainer _blobContainer;

        public BlobStorageDeploymentRepository(CloudBlobContainer blobContainer)
        {
            _blobContainer = blobContainer;
        }

        public BlobStorageDeploymentRepository(string connectionString) : this(GetApplicationsContainerReference(connectionString))
        {
        }

        private static CloudBlobContainer GetApplicationsContainerReference(string connectionString)
        {
            CloudBlobContainer blobContainer = BlobUtils.GetBlobContainer(connectionString,
                Constants.ApplicationsRootFolderName);
            if (!blobContainer.Exists())
            {
                blobContainer.Create();
            }
            return blobContainer;
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

        public async Task DownloadApplicationBinaries(AppIdentity appIdentity, string localPath,
            ConflictResolutionMode conflictResolutionMode)
        {
            bool exists = !FileUtils.DirectoryDoesntExistOrEmpty(localPath);
            if (exists)
            {
                if (conflictResolutionMode == ConflictResolutionMode.DoNothingIfBinariesExist)
                {
                    return;
                }
                if (conflictResolutionMode == ConflictResolutionMode.FailIfBinariesExist)
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

        public async Task UploadApplicationBinaries(AppIdentity appIdentity, string localPath,
            ConflictResolutionMode conflictResolutionMode)
        {
            if (FileUtils.DirectoryDoesntExistOrEmpty(localPath))
            {
                throw new BinariesNotFoundException(
                    $"Binaries were not be uploaded because they were not found at the given path {localPath}");
            }

            if (conflictResolutionMode == ConflictResolutionMode.OverwriteExistingBinaries)
            {
                await DeleteApplicationBinaries(appIdentity);
            }
            else
            {
                bool exists = await HasApplicationBinaries(appIdentity);

                if (exists)
                {
                    if (conflictResolutionMode == ConflictResolutionMode.DoNothingIfBinariesExist)
                    {
                        return;
                    }

                    if (conflictResolutionMode == ConflictResolutionMode.FailIfBinariesExist)
                    {
                        throw new DuplicateBinariesException(
                            $"Cannot override binaries when flag {ConflictResolutionMode.FailIfBinariesExist} is used");
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