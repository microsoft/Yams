using System.Threading.Tasks;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Application
{
    public interface IApplicationFactory
    {
        Task<IApplication> CreateApplication(AppInstallConfig appInstallConfig, string appPath);
    }
    }
