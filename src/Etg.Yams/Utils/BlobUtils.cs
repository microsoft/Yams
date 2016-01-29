using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Utils
{
    public static class BlobUtils
    {
        public static Task DownloadBlobDirectory(CloudBlobDirectory blobDirectory, string destPath)
        {
            return DownloadBlobs((dynamic) blobDirectory, destPath);
        }

        public static Task DownloadBlobContainer(CloudBlobContainer blobContainer, string destPath)
        {
            return DownloadBlobs((dynamic) blobContainer, destPath);
        }

        private static async Task DownloadBlobs(dynamic blobDirectory, string destPath)
        {
            await FileUtils.DeleteDirectoryIfAny(destPath);
            await FileUtils.CreateDirectory(destPath);

            IEnumerable<IListBlobItem> blobs = await ListBlobsFlat(blobDirectory);
            var tasks = new List<Task>();
            foreach (var blobItem in blobs)
            {
                var blob = (CloudBlockBlob) blobItem;
                await blob.FetchAttributesAsync();
                var localPathBlob = Path.Combine(destPath, GetLocalRelativePath(blob, blobDirectory));
                string dirPath = Path.GetDirectoryName(localPathBlob);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                tasks.Add(blob.DownloadToFileAsync(localPathBlob, FileMode.Create));
            }
            await Task.WhenAll(tasks);
        }

        private static Task<IEnumerable<IListBlobItem>> ListBlobsFlat(dynamic blobDirectory,
            bool useFlatBlobListing = true)
        {
            return Task.Run(() => (IEnumerable<IListBlobItem>) blobDirectory.ListBlobs(useFlatBlobListing));
        }

        public static Task<IEnumerable<IListBlobItem>> ListBlobsAsync(this CloudBlobDirectory blobDirectory,
            bool useFlatBlobListing = true)
        {
            return ListBlobsFlat(blobDirectory, useFlatBlobListing);
        }


        private static string GetLocalRelativePath(ICloudBlob blob, dynamic blobDirectory)
        {
            return GetBlobRelativePath(blob, blobDirectory).Replace('/', '\\');
        }

        private static string GetBlobRelativePathInternal(ICloudBlob blob, dynamic parentDirectory)
        {
            return blob.Uri.LocalPath.Remove(0, parentDirectory.Uri.LocalPath.Length).TrimStart('/');
        }

        public static string GetBlobRelativePath(ICloudBlob blob, CloudBlobDirectory parentDirectory)
        {
            return GetBlobRelativePathInternal(blob, parentDirectory);
        }

        public static string GetBlobRelativePath(ICloudBlob blob, CloudBlobContainer container)
        {
            return GetBlobRelativePathInternal(blob, container);
        }

        public static async Task CreateEmptyBlob(ICloudBlob blob)
        {
            var emptyByteArray = new byte[] {};
            await blob.UploadFromByteArrayAsync(emptyByteArray, 0, emptyByteArray.Length);
            await blob.FetchAttributesAsync();
        }

        public static Task UploadFile(string localPath, CloudBlobContainer blobContainer, string blobPath)
        {
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(blobPath);
            return blob.UploadFromFileAsync(localPath, FileMode.Open);
        }

        public static Task UploadDirectory(string localDirPath, CloudBlobContainer blobContainer, string blobDirPath)
        {
            var tasks = new List<Task>();
            foreach (string filePath in FileUtils.ListFilesRecursively(localDirPath))
            {
                string relPath = FileUtils.GetRelativePath(localDirPath, filePath);
                string blobPath = Path.Combine(blobDirPath, relPath).Replace('\\', '/');
                tasks.Add(UploadFile(filePath, blobContainer, blobPath));
            }
            return Task.WhenAll(tasks);
        }

        public static CloudBlobContainer GetBlobContainer(string connectionString, string containerName)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            return blobClient.GetContainerReference(containerName);
        }

        public static Task<bool> ExistsAsync(this CloudBlobDirectory dir)
        {
            return Task.Run(() => dir.ListBlobs().Any());
        }

        public static Task DeleteAsync(this CloudBlobDirectory dir)
        {
            return Task.Run(() =>
            {
                IEnumerable<IListBlobItem> blobs = dir.ListBlobs(true);
                var tasks = new List<Task>();
                foreach (IListBlobItem blobItem in blobs)
                {
                    CloudBlockBlob blob = (CloudBlockBlob) blobItem;
                    tasks.Add(blob.DeleteAsync());
                }
                return Task.WhenAll(tasks);
            });
        }
    }
}