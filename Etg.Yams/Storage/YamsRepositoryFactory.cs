using Etg.Yams.Utils;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Storage
{
    public class YamsRepositoryFactory : IYamsRepositoryFactory
    {
        public IYamsRepository CreateRepository(string connectionString)
        {
            CloudBlobContainer blobContainer = BlobUtils.GetBlobContainer(connectionString,
                Constants.ApplicationsRootFolderName);
            if (!blobContainer.Exists())
            {
                blobContainer.Create();
            }
            return new YamsRepository(blobContainer);
        }
    }
}