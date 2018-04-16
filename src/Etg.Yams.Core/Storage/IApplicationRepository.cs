using System.Threading.Tasks;
using Etg.Yams.Application;

namespace Etg.Yams.Storage
{
    public interface IApplicationRepository
    {
        Task UploadApplicationBinaries(AppIdentity appIdentity, string localPath, ConflictResolutionMode conflictResolutionMode);
        Task DeleteApplicationBinaries(AppIdentity appIdentity);
        Task<bool> HasApplicationBinaries(AppIdentity appIdentity);
        Task DownloadApplicationBinaries(AppIdentity appIdentity, string localPath, ConflictResolutionMode conflictResolutionMode);
    }
}