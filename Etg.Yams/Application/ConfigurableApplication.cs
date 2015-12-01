using System.Threading.Tasks;
using Etg.Yams.Process;

namespace Etg.Yams.Application
{
    public class ConfigurableApplication : Application
    {
        private readonly ApplicationConfig _appConfig;
        private readonly IProcessFactory _processFactory;
        private readonly IProcessStopper _processStopper;
        private IProcess _process;

        /// <summary>
        /// A configurable application is a generic application that can be started using a given exe and exe args.
        /// </summary>
        /// <param name="path">The path of the directory where the exe is located</param>
        /// <param name="appConfig">exe name, args, etc.</param>
        /// <param name="processFactory">A factory to create a process to run the exe</param>
        /// <param name="processStopper">Used to stop a process</param>
        public ConfigurableApplication(string path, ApplicationConfig appConfig, IProcessFactory processFactory, IProcessStopper processStopper) 
            : base(appConfig.Identity, path)
        {
            _appConfig = appConfig;
            _processFactory = processFactory;
            _processStopper = processStopper;
        }

        public override Task<bool> Start()
        {
            _process = _processFactory.CreateProcess(System.IO.Path.Combine(Path, _appConfig.ExeName), _appConfig.ExeArgs);
            return StartProcess(_process);
        }

        public override Task Stop()
        {
            return _processStopper.StopProcess(_process);
        }

        public override void Dispose()
        {
            _process.Dispose();
        }
    }
}
