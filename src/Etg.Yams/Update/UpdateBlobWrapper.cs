using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Utils;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Update
{
    /// <summary>
    /// A wrapper that encapsulates the interaction with the update blob. The update blob is a blob that role instances use 
    /// for synchronization.
    /// </summary>
    public class UpdateBlobWrapper
    {
        public const string UpdateDomainKey = "UpdateDomain";
        public const string UpdateDomainInstancesKey = "UpdateDomainInstances";
        private readonly ICloudBlob _blob;

        private UpdateBlobWrapper(ICloudBlob blob)
        {
            _blob = blob;
        }

        public static async Task<UpdateBlobWrapper> Create(ICloudBlob blob)
        {
            UpdateBlobWrapper updateBlobWrapper = new UpdateBlobWrapper(blob);
            await updateBlobWrapper.FetchOrCreate();
            return updateBlobWrapper;
        }

        public Task SetData(string leaseId, string updateDomain, IEnumerable<string> instanceIds)
        {
            _blob.Metadata[UpdateDomainKey] = updateDomain;
            if (instanceIds.Any())
            {
                _blob.Metadata[UpdateDomainInstancesKey] = string.Join(",", instanceIds);
            }
            else
            {
                _blob.Metadata.Remove(UpdateDomainInstancesKey);
            }
            return _blob.SetMetadataAsync(
                new AccessCondition
                {
                    LeaseId = leaseId
                }, new BlobRequestOptions(), new OperationContext());
        }

        public string GetUpdateDomain()
        {
            return _blob.Metadata.ContainsKey(UpdateDomainKey)? _blob.Metadata[UpdateDomainKey] : string.Empty;
        }

        public HashSet<string> GetInstanceIds()
        {
            var updateDomainInstancesHashset = new HashSet<string>();
            if (_blob.Metadata.ContainsKey(UpdateDomainInstancesKey))
            {
                var updateDomainInstances = _blob.Metadata[UpdateDomainInstancesKey].Split(',');
                updateDomainInstancesHashset = new HashSet<string>(updateDomainInstances);
            }
            
            return updateDomainInstancesHashset;
        }

        private async Task FetchOrCreate()
        {
            if (!await _blob.ExistsAsync())
            {
                await BlobUtils.CreateEmptyBlob(_blob);
            }
        }
    }
}
