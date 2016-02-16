using System.Threading.Tasks;

namespace Etg.Yams.Application
{
    public interface IApplicationFactory
    {
        Task<IApplication> CreateApplication(AppIdentity appIdentity, string appPath);
    }
    }
