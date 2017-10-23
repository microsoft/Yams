using System;
using CommandLine;
using Etg.Yams.Ipc;

namespace Etg.Yams.Client
{
    public class YamsClientFactory : IYamsClientFactory
    {
        private readonly IProcessArgsParser _processArgsParser;

        public YamsClientFactory() : this(new ProcessArgsParser())
        {
        }

        public YamsClientFactory(IProcessArgsParser processArgsParser)
        {
            _processArgsParser = processArgsParser;
        }

        public IYamsClient CreateYamsClient(YamsClientConfig config)
        {
            var options = _processArgsParser.ParseArgs(config.ProcessArgs);

            IpcConnection initConnection = null;
            IpcConnection exitConnection = null;
            IpcConnection healthConnection = null;

            if (!string.IsNullOrEmpty(options.InitializationPipeName))
            {
                initConnection = new IpcConnection(new NamedPipeClientAdapter(options.InitializationPipeName));
            }
            if (!string.IsNullOrEmpty(options.ExitPipeName))
            {
                exitConnection = new IpcConnection(new NamedPipeClientAdapter(options.ExitPipeName));
            }
            if (!string.IsNullOrEmpty(options.HealthPipeName))
            {
                healthConnection = new IpcConnection(new NamedPipeClientAdapter(options.HealthPipeName));
            }
            return new YamsClient(config, initConnection, exitConnection, healthConnection);
        }
    }
}