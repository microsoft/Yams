using System.Threading.Tasks;

namespace Etg.Yams.Azure.UpdateSession
{
    public interface IUpdateBlobFactory
    {
        Task<IUpdateBlob> TryLockUpdateBlob(string appId);
    }
}