using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Etg.Yams.Azure.Lease;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Azure.UpdateSession
{
    public class UpdateBlob : IUpdateBlob
    {
        private const string UpdateDomainKey = "UpdateDomain";
        private const string UpdateDomainInstancesKey = "UpdateDomainInstances";

        private readonly ICloudBlob _blob;
        private readonly IBlobLeaseFactory _blobLeaseFactory;
        private string _updateDomain;
        private HashSet<string> _instanceIds;
        private string _leaseId;
        private IBlobLease _lease;

        public UpdateBlob(ICloudBlob blob, IBlobLeaseFactory blobLeaseFactory)
        {
            _blob = blob;
            _blobLeaseFactory = blobLeaseFactory;
            _updateDomain = string.Empty;
            _instanceIds = new HashSet<string>();
        }

        public void SetUpdateDomain(string updateDomain)
        {
            EnsureThatBlobIsLocked();
            _updateDomain = updateDomain;
        }

        private void EnsureThatBlobIsLocked()
        {
            if (_lease == null)
            {
                throw new InvalidOperationException("The updated blob must be locked before being accessed");
            }
        }

        public void AddInstance(string instanceId)
        {
            EnsureThatBlobIsLocked();
            _instanceIds.Add(instanceId);
        }

        public void RemoveInstance(string instanceId)
        {
            EnsureThatBlobIsLocked();
            _instanceIds.Remove(instanceId);
        }

        public string GetUpdateDomain()
        {
            EnsureThatBlobIsLocked();
            return _updateDomain;
        }

        public ISet<string> GetInstanceIds()
        {
            EnsureThatBlobIsLocked();
            return _instanceIds;
        }

        public async Task<bool> TryLock()
        {
            try
            {
                _lease = _blobLeaseFactory.CreateLease(_blob);
                _leaseId = await _lease.TryAcquireLease();
                if (_leaseId == null)
                {
                    DisposeLease();
                    return false;
                }
                await _blob.FetchAttributesAsync();

                if (_blob.Metadata.ContainsKey(UpdateDomainInstancesKey))
                {
                    string[] instanceIds = _blob.Metadata[UpdateDomainInstancesKey].Split(',');
                    _instanceIds = new HashSet<string>(instanceIds);
                }
                _updateDomain = _blob.Metadata.ContainsKey(UpdateDomainKey) ? _blob.Metadata[UpdateDomainKey] : string.Empty;

                return true;
            }
            catch (StorageException e)
            {
                DisposeLease();
                Trace.TraceInformation($"Failed to acquire the lease on blob {_blob.Name}", e);
                return false;
            }
        }

        private void DisposeLease()
        {
            _leaseId = null;
            _lease.Dispose();
            _lease = null;
        }

        private Task FlushBlobMetadata()
        {
            if (_instanceIds.Any())
            {
                if (string.IsNullOrEmpty(_updateDomain))
                {
                    throw new InvalidOperationException("Update domain should be set before setting instance ids");
                }
                _blob.Metadata[UpdateDomainInstancesKey] = string.Join(",", _instanceIds);
                _blob.Metadata[UpdateDomainKey] = _updateDomain;
            }
            else
            {
                _blob.Metadata.Remove(UpdateDomainKey);
                _blob.Metadata.Remove(UpdateDomainInstancesKey);
            }
            return _blob.SetMetadataAsync(
                new AccessCondition
                {
                    LeaseId = _leaseId
                }, new BlobRequestOptions(), new OperationContext());
        }

        public async Task FlushAndRelease()
        {
            EnsureThatBlobIsLocked();
            await FlushBlobMetadata();
            await Release();
        }

        public async Task Release()
        {
            await _lease.ReleaseLease();
            DisposeLease();
        }

        public void Dispose()
        {
            if (_lease != null)
            {
                Release().Wait();
            }
        }
    }
}