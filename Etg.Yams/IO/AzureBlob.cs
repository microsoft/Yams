using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.IO
{
    public class AzureBlob : IRemoteFile
    {
        private readonly CloudBlockBlob _cloudBlockBlob;

        public AzureBlob(CloudBlockBlob cloudBlockBlob)
        {
            _cloudBlockBlob = cloudBlockBlob;
        }

        public string Name
        {
            get { return Path.GetFileName(_cloudBlockBlob.Name); }
        }

        public string Uri
        {
            get { return _cloudBlockBlob.Uri.ToString(); }
        }

        public Task<string> DownloadText()
        {
            return Task.Run(() => _cloudBlockBlob.DownloadText());
        }

        public Task Download(string destPath)
        {
            return _cloudBlockBlob.DownloadToFileAsync(destPath, FileMode.Create);
        }

        public Task<bool> Exists()
        {
            return _cloudBlockBlob.ExistsAsync();
        }
    }
}