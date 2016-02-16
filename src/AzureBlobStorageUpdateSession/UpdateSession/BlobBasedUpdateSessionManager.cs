using System.Diagnostics;
using System.Threading.Tasks;
using Etg.Yams.Update;

namespace Etg.Yams.Azure.UpdateSession
{
    public class BlobBasedUpdateSessionManager : IUpdateSessionManager
    {
        private readonly IUpdateBlobFactory _updateBlobFactory;
        private readonly string _instanceId;
        private readonly string _instanceUpdateDomain;

        public BlobBasedUpdateSessionManager(IUpdateBlobFactory updateBlobFactory, string instanceId, string instanceUpdateDomain)
        {
            _updateBlobFactory = updateBlobFactory;
            _instanceId = instanceId;
            _instanceUpdateDomain = instanceUpdateDomain;
        }

        public async Task<bool> TryStartUpdateSession(string applicationId)
        {
            using (IUpdateBlob updateBlob = await _updateBlobFactory.TryLockUpdateBlob(applicationId))
            {
                string updateDomain = updateBlob.GetUpdateDomain();

                if (string.IsNullOrEmpty(updateDomain) || _instanceUpdateDomain == updateDomain)
                {
                    updateDomain = _instanceUpdateDomain;
                    Trace.TraceInformation(
                        $"Instance {_instanceId} will attempt to start update session for " +
                        $"ApplicationId = {applicationId}, UpdateDomain = {updateDomain}");

                    updateBlob.SetUpdateDomain(updateDomain);
                    updateBlob.AddInstance(_instanceId);
                    await updateBlob.FlushAndRelease();

                    Trace.TraceInformation(
                        $"Instance {_instanceId} successfully started the update session for " +
                        $"ApplicationId = {applicationId}, UpdateDomain = {updateDomain}");
                    return true;
                }
            }
            return false;
        }

        public async Task EndUpdateSession(string applicationId)
        {
            using (IUpdateBlob updateBlob = await _updateBlobFactory.TryLockUpdateBlob(applicationId))
            {
                Trace.TraceInformation(
                    $"Instance {_instanceId} Will attempt to end the update session for " +
                    $"ApplicationId = {applicationId}, " +
                    $"UpdateDomain = {_instanceUpdateDomain}");

                updateBlob.RemoveInstance(_instanceId);
                await updateBlob.FlushAndRelease();

                Trace.TraceInformation(
                    $"Instance {_instanceId} successfully ended the update session for " +
                    $"ApplicationId = {applicationId}, " +
                    $"UpdateDomain = {_instanceUpdateDomain}");
            }
        }
    }
}
