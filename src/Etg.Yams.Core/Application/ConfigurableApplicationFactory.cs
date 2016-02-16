using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Process;

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

        public async Task<IApplication> CreateApplication(AppIdentity appIdentity, string appPath)
        {
            ApplicationConfig appConfig = await _appConfigParser.ParseFile(Path.Combine(appPath, Constants.AppConfigFileName), appIdentity);
            return new ConfigurableApplication(appPath, appConfig, _processFactory, _processStopper);
        }
    }
}
