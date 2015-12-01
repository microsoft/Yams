using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Utils
{
    public static class BlobUtils
    {
        public static Task DownloadBlobDirectory(CloudBlobDirectory blobDirectory, string destPath)
        {
            return DownloadBlobs((dynamic)blobDirectory, destPath);
        }

        public static Task DownloadBlobContainer(CloudBlobContainer blobContainer, string destPath)
        {
            return DownloadBlobs((dynamic)blobContainer, destPath);
        }

        private static async Task DownloadBlobs(dynamic blobDirectory, string destPath)
        {
            await FileUtils.DeleteDirectoryIfAny(destPath);
            await FileUtils.CreateDirectory(destPath);

            IEnumerable<IListBlobItem> blobs = await ListBlobsFlat(blobDirectory);
            var tasks = new List<Task>();
            foreach (var blobItem in blobs)
            {
                var blob = (CloudBlockBlob)blobItem;
                await blob.FetchAttributesAsync();
                var localPathBlob = Path.Combine(destPath, GetRelativePath(blob, blobDirectory));
                string dirPath = Path.GetDirectoryName(localPathBlob);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                tasks.Add(blob.DownloadToFileAsync(localPathBlob, FileMode.Create));
            }
            await Task.WhenAll(tasks);
        }

        private static Task<IEnumerable<IListBlobItem>> ListBlobsFlat(dynamic blobDirectory)
        {
            return Task.Run(() => (IEnumerable<IListBlobItem>)blobDirectory.ListBlobs(useFlatBlobListing: true));
        }

        private static string GetRelativePath(ICloudBlob blob, dynamic blobDirectory)
        {
            return blob.Uri.LocalPath.Remove(0, blobDirectory.Uri.LocalPath.Length).Replace('/', '\\');
        }

        public static async Task CreateEmptyBlob(ICloudBlob blob)
        {
            var emptyByteArray = new byte[] { };
            await blob.UploadFromByteArrayAsync(emptyByteArray, 0, emptyByteArray.Length);
            await blob.FetchAttributesAsync();
        }
    }
}

