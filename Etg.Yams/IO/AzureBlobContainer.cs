using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Utils;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.IO
{
    public class AzureBlobContainer : IRemoteDirectory
    {
        private readonly CloudBlobContainer _blobContainer;

        public AzureBlobContainer(CloudBlobContainer blobContainer)
        {
            _blobContainer = blobContainer;
        }

        public string Name
        {
            get { return _blobContainer.Name; }
        }

        public string Uri
        {
            get { return _blobContainer.Uri.ToString(); }
        }

        public Task<IEnumerable<IRemoteDirectory>> ListDirectories()
        {
            return Task.Run(() =>
            {
                IEnumerable<IRemoteDirectory> directories = _blobContainer.ListBlobs().OfType<CloudBlobDirectory>().Select(blobDir => new AzureBlobDirectory(blobDir));
                return directories;
            });
        }

        public Task<IEnumerable<IRemoteFile>> ListFiles()
        {
            return Task.Run(() =>
            {
                IEnumerable<IRemoteFile> files = _blobContainer.ListBlobs().OfType<CloudBlockBlob>().Select(blob => new AzureBlob(blob));
                return files;
            });
        }

        public Task<IRemoteDirectory> GetDirectory(string name)
        {
            return Task.Run(() =>
            {
                IRemoteDirectory directory = new AzureBlobDirectory(_blobContainer.GetDirectoryReference(name));
                return directory;
            });
        }

        public Task<IRemoteFile> GetFile(string name)
        {
            return Task.Run(() =>
            {
                IRemoteFile file = new AzureBlob(_blobContainer.GetBlockBlobReference(name));
                return file;
            });
        }

        public Task<bool> Exists()
        {
            return Task.Run(() => _blobContainer.Exists() && _blobContainer.ListBlobs().Any());
        }

        public Task Download(string destPath)
        {
            return BlobUtils.DownloadBlobContainer(_blobContainer, destPath);
        }
    }
}