using System.Diagnostics;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Azure.Utils;
using Etg.Yams.Storage;
using Etg.Yams.Storage.Config;
using Etg.Yams.Utils;
using Microsoft.WindowsAzure.Storage.Blob;
using Etg.Yams.Json;
using Newtonsoft.Json.Serialization;

namespace Etg.Yams.Azure.Storage
{
    public class BlobStorageDeploymentRepository : IDeploymentRepository
    {
        public const string ApplicationsRootFolderName = "applications";
        private readonly CloudBlobContainer _blobContainer;
        private readonly IDeploymentConfigSerializer _serializer;

        public BlobStorageDeploymentRepository(CloudBlobContainer blobContainer, IDeploymentConfigSerializer serializer)
        {
            _blobContainer = blobContainer;
            _serializer = serializer;
        }

        public BlobStorageDeploymentRepository(string connectionString, IDeploymentConfigSerializer serializer) 
            : this(GetApplicationsContainerReference(connectionString), serializer)
        {
        }

        public static BlobStorageDeploymentRepository Create(string connectionString)
        {
            IDeploymentConfigSerializer serializer = new JsonDeploymentConfigSerializer(new JsonSerializer(new DiagnosticsTraceWriter()));
            return new BlobStorageDeploymentRepository(connectionString, serializer);
        }

        private static CloudBlobContainer GetApplicationsContainerReference(string connectionString)
        {
            CloudBlobContainer blobContainer = BlobUtils.GetBlobContainer(connectionString,
                ApplicationsRootFolderName);
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
            return _serializer.Deserialize(data);
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
            return blob.UploadTextAsync(_serializer.Serialize(deploymentConfig));
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