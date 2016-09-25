using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Test.stubs
{
    public class ApplicationFactoryStub : IApplicationFactory
    {
        public Task<IApplication> CreateApplication(AppInstallConfig appInstallConfig, string appPath)
        {
            return Task.FromResult((IApplication)new ApplicationStub(appInstallConfig.AppIdentity, appPath));
        }
    }
}
