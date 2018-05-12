using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Etg.Yams.Utils;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Azure.Utils
{
    public static class BlobUtils
    {
        public static async Task DownloadBlobDirectory(CloudBlobDirectory blobDirectory, string destPath)
        {
            if (!Directory.Exists(destPath))
            {
                await FileUtils.CreateDirectory(destPath);
            }

            IEnumerable<IListBlobItem> blobs = await ListBlobsFlat(blobDirectory);
            var tasks = new List<Task>();

            using (var md5 = new MD5CryptoServiceProvider())
            {
                foreach (var blobItem in blobs)
                {
                    var blob = (CloudBlockBlob)blobItem;
                    await blob.FetchAttributesAsync();
                    string relativePath = GetLocalRelativePath(blob, blobDirectory);
                    var localFilePath = Path.Combine(destPath, relativePath);
                    string dirPath = Path.GetDirectoryName(localFilePath);
                    if (!Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }
                    if (File.Exists(localFilePath))
                    {
                        string hash = ComputeMd5Hash(md5, localFilePath);
                        if (hash == blob.Properties.ContentMD5)
                        {
                            continue;
                        }
                    }
                    tasks.Add(blob.DownloadToFileAsync(localFilePath, FileMode.Create));
                }
            }
            await Task.WhenAll(tasks);
        }

        private static string ComputeMd5Hash(MD5CryptoServiceProvider md5, string localFilePath)
        {
            using (var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
            {
                return Convert.ToBase64String(md5.ComputeHash(stream));
            }
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

        public static async Task CreateBlobIfNotExists(ICloudBlob blob)
        {
            var emptyByteArray = new byte[] { };
            try
            {
                await blob.UploadFromByteArrayAsync(emptyByteArray, 0, emptyByteArray.Length,
                    AccessCondition.GenerateIfNotExistsCondition(), new BlobRequestOptions(), new OperationContext());
            } catch(StorageException e)
            {
                if(e.RequestInformation.HttpStatusCode != 409)
                {
                    throw;
                }
            }
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
            return blob.UploadFromFileAsync(localPath);
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
            var blobContainer = blobClient.GetContainerReference(containerName);
            if (!blobContainer.Exists())
            {
                blobContainer.Create();
            }
            return blobContainer;
        }

        public static async Task<bool> ExistsAsync(this CloudBlobDirectory dir)
        {
            return (await dir.ListBlobsAsync()).Any();
        }

        public static async Task DeleteAsync(this CloudBlobDirectory dir)
        {
            IEnumerable<IListBlobItem> blobs = await dir.ListBlobsAsync(useFlatBlobListing:true);
            var tasks = new List<Task>();
            foreach (IListBlobItem blobItem in blobs)
            {
                CloudBlockBlob blob = (CloudBlockBlob) blobItem;
                tasks.Add(blob.DeleteAsync());
            }
            await Task.WhenAll(tasks);
        }
    }
}