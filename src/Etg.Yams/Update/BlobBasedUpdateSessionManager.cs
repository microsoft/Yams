using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Etg.Yams.Lease;
using Etg.Yams.Utils;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Etg.Yams.Update
{
    public class BlobBasedUpdateSessionManager : IUpdateSessionManager
    {
        private readonly UpdateSessionManagerConfig _config;
        private readonly CloudBlobClient _blobClient;
        private readonly IBlobLeaseFactory _blobLeaseFactory;

        public BlobBasedUpdateSessionManager(UpdateSessionManagerConfig config, CloudBlobClient blobClient, IBlobLeaseFactory blobLeaseFactory)
        {
            _config = config;
            _blobClient = blobClient;
            _blobLeaseFactory = blobLeaseFactory;
        }

        public async Task<bool> TryStartUpdateSession(string applicationId)
        {
            string updateBlobName = GetUpdateBlobName(applicationId);
            bool started = false;
            try
            {
                ICloudBlob updateBlob = GetBlob(updateBlobName);
                await CreateBlobIfNoneExists(updateBlob);

                IBlobLease blobLease = _blobLeaseFactory.CreateLease(updateBlob);

                ExceptionDispatchInfo capturedException = null;
                string leaseId = await blobLease.TryAcquireLease();
                // allow this instance to update only if it can acquire a lease on the blob. 
                if (leaseId == null)
                {
                    Trace.TraceInformation("Cannot start update session because unable to acquire the lease on the blob");
                    return false;
                }

                try
                {
                    var updateBlobWrapper = await UpdateBlobWrapper.Create(updateBlob);
                    var updateDomain = updateBlobWrapper.GetUpdateDomain();
                    HashSet<string> updateDomainInstancesHashset = updateBlobWrapper.GetInstanceIds();

                    // allow this instance to try updating only if either one of the following conditions are true:
                    // 1. no update domain is set,
                    // 2. the current instance is in the same update domain
                    // 3. no instances are set for the current update domain   
                    if (string.IsNullOrEmpty(updateDomain) ||
                        _config.InstanceUpdateDomain == updateDomain ||
                        updateDomainInstancesHashset.Count == 0)
                    {
                        Trace.TraceInformation(
                            "Instance {0} acquired a lock on update blob {1} and will attempt to enlist itself",
                            _config.InstanceId, updateBlobName);

                        updateDomainInstancesHashset.Add(_config.InstanceId);
                        await
                            updateBlobWrapper.SetData(leaseId, _config.InstanceUpdateDomain,
                                updateDomainInstancesHashset);

                        Trace.TraceInformation("Instance {0} enlisted itself in the update blob", _config.InstanceId);
                        started = true;
                    }
                }
                catch (Exception e)
                {
                    capturedException = ExceptionDispatchInfo.Capture(e);
                }

                await blobLease.ReleaseLease();
                Trace.TraceInformation("Instance {0} released the lock on update blob {1}", _config.InstanceId, updateBlobName);
                    
                if (capturedException != null)
                {
                    capturedException.Throw();
                }

                return started;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to start update session ", ex);
            }
        }

        private static async Task CreateBlobIfNoneExists(ICloudBlob updateBlob)
        {
            if (! await updateBlob.ExistsAsync())
            {
                await BlobUtils.CreateEmptyBlob(updateBlob);
            }
        }

        public async Task EndUpdateSession(string applicationId)
        {
            string updateBlobName = GetUpdateBlobName(applicationId);
            var retryAttemptsRemaining = 10;
            var retryAttempts = retryAttemptsRemaining;
            bool failed = false;
            do
            {
                if (retryAttemptsRemaining-- == 0)
                {
                    throw new Exception(
                        string.Format("Instance {0} failed to update the update blob {1} after trying {2} times",
                            _config.InstanceId,
                            updateBlobName,
                            retryAttempts));
                }

                try
                {
                    ICloudBlob updateBlob = GetBlob(updateBlobName);
                    IBlobLease blobLease = _blobLeaseFactory.CreateLease(updateBlob);

                    ExceptionDispatchInfo capturedException = null;
                    string leaseId = await blobLease.TryAcquireLease();
                    if (leaseId != null)
                    {
                        Trace.TraceInformation(
                            "Instance {0} acquired a lock on update blob {1} and will attempt to delist itself",
                            _config.InstanceId, updateBlobName);
                        try
                        {
                            UpdateBlobWrapper updateBlobWrapper = await UpdateBlobWrapper.Create(updateBlob);
                            var updateDomain = updateBlobWrapper.GetUpdateDomain();
                            var updateDomainInstancesHashset = updateBlobWrapper.GetInstanceIds();

                            updateDomainInstancesHashset.Remove(_config.InstanceId);
                            await updateBlobWrapper.SetData(leaseId, updateDomain, updateDomainInstancesHashset);

                            Trace.TraceInformation("Instance {0} delisted itself from the update blob", _config.InstanceId);
                        }
                        catch (Exception e)
                        {
                            capturedException = ExceptionDispatchInfo.Capture(e);
                        }
                        finally
                        {
                            Trace.TraceInformation("Instance {0} released the lock on update blob {1}",
                                _config.InstanceId, updateBlobName);
                        }

                        await blobLease.ReleaseLease();

                        if (capturedException != null)
                        {
                            capturedException.Throw();
                        }
                    }
                    else
                    {
                        throw new Exception(
                            "Update session manager failed to end update session because it cannot Acquire the lease");
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Instance {0} failed to update the update blob {1}. Exception was: {2}",
                        _config.InstanceId, updateBlobName, ex);
                    failed = true;
                }
                if (failed)
                {
                    await Task.Delay(1000); // allowing 1s between retries
                }
            } while (failed);
        }

        private CloudBlockBlob GetBlob(string updateBlobName)
        {
            return _blobClient.GetContainerReference(_config.StorageContainerName).GetBlockBlobReference(updateBlobName);
        }

        private string GetUpdateBlobName(string applicationId)
        {
            return _config.CloudServiceDeploymentId + "_" + applicationId + "_update_blob";
        }
    }
}
