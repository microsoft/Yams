using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Utils;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.IO
{
    public class AzureBlobDirectory : IRemoteDirectory
    {
        private readonly CloudBlobDirectory _cloudBlobDirectory;

        public AzureBlobDirectory(CloudBlobDirectory cloudBlobDirectory)
        {
            _cloudBlobDirectory = cloudBlobDirectory;
        }

        public string Name
        {
            get { return _cloudBlobDirectory.Uri.Segments.Last().Replace("/", string.Empty); }
        }

        public string Uri
        {
            get { return _cloudBlobDirectory.Uri.ToString(); }
        }

        public Task<IEnumerable<IRemoteDirectory>> ListDirectories()
        {
            return Task.Run(() =>
            {
                IEnumerable<IRemoteDirectory> directories = _cloudBlobDirectory.ListBlobs().OfType<CloudBlobDirectory>().Select(blobDir => new AzureBlobDirectory(blobDir));
                return directories;
            });
        }

        public Task<IEnumerable<IRemoteFile>> ListFiles()
        {
            return Task.Run(() =>
            {
                IEnumerable<IRemoteFile> files = _cloudBlobDirectory.ListBlobs().OfType<CloudBlockBlob>().Select(blob => new AzureBlob(blob));
                return files;
            });
        }

        public Task<IRemoteDirectory> GetDirectory(string name)
        {
                return Task.Run(() =>
                {
                    IRemoteDirectory directory = new AzureBlobDirectory(_cloudBlobDirectory.GetDirectoryReference(name));
                    return directory;
                });
        }

        public Task<IRemoteFile> GetFile(string name)
        {
            return Task.Run(() =>
            {
                IRemoteFile file = new AzureBlob(_cloudBlobDirectory.GetBlockBlobReference(name));
                return file;

            });
        }

        public Task<bool> Exists()
        {
            return Task.Run(() =>_cloudBlobDirectory.ListBlobs().Any());
        }

        public Task Download(string destPath)
        {
            return BlobUtils.DownloadBlobDirectory(_cloudBlobDirectory, destPath);
        }
    }
}