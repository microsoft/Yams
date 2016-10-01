using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Install;
using Etg.Yams.Process;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Application
{
    public class ConfigurableApplicationFactory : IApplicationFactory
    {
        private readonly IApplicationConfigParser _appConfigParser;
        private readonly IProcessFactory _processFactory;
        private readonly IProcessStopper _processStopper;

        public ConfigurableApplicationFactory(IApplicationConfigParser appConfigParser, IProcessFactory processFactory, IProcessStopper processStopper)
        {
            _appConfigParser = appConfigParser;
            _processFactory = processFactory;
            _processStopper = processStopper;
        }

        public async Task<IApplication> CreateApplication(AppInstallConfig appInstallConfig, string appPath)
        {
            string appConfigPath = Path.Combine(appPath, Constants.AppConfigFileName);
            ApplicationConfig appConfig = await _appConfigParser.ParseFile(appConfigPath, appInstallConfig);
            return new ConfigurableApplication(appPath, appConfig, _processFactory, _processStopper);
        }
    }
}
