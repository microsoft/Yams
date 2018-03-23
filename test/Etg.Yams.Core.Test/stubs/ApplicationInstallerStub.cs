using System.Collections.Generic;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Install;

namespace Etg.Yams.Test.stubs
{
    public class ApplicationInstallerStub : IApplicationInstaller
    {
        private readonly IApplicationPool _applicationPool;
        private readonly string path;

        public ApplicationInstallerStub(IApplicationPool applicationPool, string path)
        {
            _applicationPool = applicationPool;
            this.path = path;
        }

        public Task Install(AppInstallConfig appInstallConfig)
        {
            _applicationPool.AddApplication(new ApplicationStub(appInstallConfig.AppIdentity, GetAppPath(appInstallConfig)));
            return Task.CompletedTask;
        }

        private string GetAppPath(AppInstallConfig appInstallConfig)
        {
            return $"{path}\\{appInstallConfig.AppIdentity.Id}\\{appInstallConfig.AppIdentity.Version}";
        }

        public Task UnInstall(AppIdentity appIdentity)
        {
            _applicationPool.RemoveApplication(appIdentity);
            return Task.CompletedTask;
        }

        public Task<bool> Update(IEnumerable<AppIdentity> applicationsToRemove, IEnumerable<AppInstallConfig> applicationsToDeploy)
        {
            foreach (var appIdentity in applicationsToRemove)
            {
                _applicationPool.RemoveApplication(appIdentity);
            }
            foreach (var appInstallConfig in applicationsToDeploy)
            {
                _applicationPool.AddApplication(new ApplicationStub(appInstallConfig.AppIdentity, "path"));
            }
            return Task.FromResult(true);
        }
    }
}
