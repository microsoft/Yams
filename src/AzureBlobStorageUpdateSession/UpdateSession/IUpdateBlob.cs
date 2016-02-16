using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Etg.Yams.Azure.UpdateSession
{
    public interface IUpdateBlob : IDisposable
    {
        void AddInstance(string instanceId);
        void RemoveInstance(string instanceId);
        string GetUpdateDomain();
        ISet<string> GetInstanceIds();
        Task<bool> TryLock();
        Task FlushAndRelease();
        void SetUpdateDomain(string updateDomain);
        Task Release();
    }
}