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
using Etg.Yams.Storage.Status;
using System;
using System.Collections.Generic;

namespace Etg.Yams.Azure.Storage
{
    public class BlobStorageDeploymentRepository : IDeploymentRepository, IDeploymentStatusReader, IDeploymentStatusWriter
    {
        public const string ApplicationsRootFolderName = "applications";
        private readonly CloudBlobContainer _blobContainer;
        private readonly IDeploymentConfigSerializer _deploymentConfigSerializer;
        private readonly IDeploymentStatusSerializer _deploymentStatusSerializer;

        public BlobStorageDeploymentRepository(CloudBlobContainer blobContainer, IDeploymentConfigSerializer serializer,
            IDeploymentStatusSerializer deploymentStatusSerializer)
        {
            _blobContainer = blobContainer;
            _deploymentConfigSerializer = serializer;
            _deploymentStatusSerializer = deploymentStatusSerializer;
        }

        public BlobStorageDeploymentRepository(string connectionString, IDeploymentConfigSerializer deploymentConfigSerializer,
            IDeploymentStatusSerializer deploymentStatusSerializer) 
            : this(GetApplicationsContainerReference(connectionString), deploymentConfigSerializer, deploymentStatusSerializer)
        {
        }

        public static BlobStorageDeploymentRepository Create(string connectionString)
        {
            var jsonSerializer = new JsonSerializer(new DiagnosticsTraceWriter());
            IDeploymentConfigSerializer deploymentConfigSerializer = new JsonDeploymentConfigSerializer(jsonSerializer);
            IDeploymentStatusSerializer deploymentStatusSerializer = new JsonDeploymentStatusSerializer(jsonSerializer);
            return new BlobStorageDeploymentRepository(connectionString, deploymentConfigSerializer, deploymentStatusSerializer);
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
            return _deploymentConfigSerializer.Deserialize(data);
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
            return blob.UploadTextAsync(_deploymentConfigSerializer.Serialize(deploymentConfig));
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

        public async Task<ClusterDeploymentStatus> FetchClusterDeploymentStatus(string clusterId, int ttlSeconds)
        {
            ClusterDeploymentStatus clusterDeploymentStatus = new ClusterDeploymentStatus();;
            CloudBlobDirectory instancesBlobDir = _blobContainer.GetDirectoryReference(GetClusterStatusRelativePath(clusterId));
            if (!await instancesBlobDir.ExistsAsync())
            {
                return clusterDeploymentStatus;
            }
            IEnumerable<IListBlobItem> instancesBlobList = await instancesBlobDir.ListBlobsAsync();
            foreach (var blob in instancesBlobList)
            {
                CloudBlockBlob instanceBlob = blob as CloudBlockBlob;
                if (instanceBlob == null)
                {
                    Trace.TraceWarning($"Unexpected blob {blob.Uri} in cluster status directory");
                    continue;
                }
                double secondsSinceModified = DateTimeOffset.UtcNow.Subtract(instanceBlob.Properties.LastModified.Value).TotalSeconds;
                if (secondsSinceModified > ttlSeconds)
                {
                    continue;
                }

                try
                {
                    InstanceDeploymentStatus instanceDeploymentStatus =
                        await FetchInstanceDeploymentStatus(instanceBlob);
                    string instanceId = instanceBlob.Name;
                    clusterDeploymentStatus.SetInstanceDeploymentStatus(instanceId, instanceDeploymentStatus);
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Could not read instance deployment status {instanceBlob.Uri}, Exception: {e}");
                }
            }
            return clusterDeploymentStatus;
        }

        public async Task<InstanceDeploymentStatus> FetchInstanceDeploymentStatus(string clusterId, string instanceId)
        {
            CloudBlockBlob instanceBlob =
                _blobContainer.GetBlockBlobReference(GetInstanceStatusRelativePath(clusterId, instanceId));
            if (!await instanceBlob.ExistsAsync())
            {
                return new InstanceDeploymentStatus();
            }
            return await FetchInstanceDeploymentStatus(instanceBlob);
        }

        private static string GetClusterStatusRelativePath(string clusterId)
        {
            return $"status/clusters/{clusterId}/instances";
        }

        private static string GetInstanceStatusRelativePath(string clusterId, string instanceId)
        {
            return $"{GetClusterStatusRelativePath(clusterId)}/{instanceId}";
        }

        private async Task<InstanceDeploymentStatus> FetchInstanceDeploymentStatus(CloudBlockBlob instanceBlob)
        {
            string data = await instanceBlob.DownloadTextAsync();
            return _deploymentStatusSerializer.Deserialize(data);
        }

        public Task PublishInstanceDeploymentStatus(string clusterId, string instanceId, 
            InstanceDeploymentStatus instanceDeploymentStatus)
        {
            CloudBlockBlob blob = _blobContainer.GetBlockBlobReference(GetInstanceStatusRelativePath(clusterId, instanceId));
            string data = _deploymentStatusSerializer.Serialize(instanceDeploymentStatus);
            return blob.UploadTextAsync(data);
        }
    }
}