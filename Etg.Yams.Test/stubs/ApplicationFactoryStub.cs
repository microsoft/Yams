using System.Threading.Tasks;
using Etg.Yams.Application;

namespace Etg.Yams.Test.stubs
{
    public class ApplicationFactoryStub : IApplicationFactory
    {
        public Task<IApplication> CreateApplication(AppIdentity appIdentity, string appPath)
        {
            return Task.FromResult((IApplication)new ApplicationStub(appIdentity, appPath));
        }
    }
}
