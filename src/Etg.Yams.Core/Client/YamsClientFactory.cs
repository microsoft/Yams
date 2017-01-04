using System;
using CommandLine;
using Etg.Yams.Process.Ipc;

namespace Etg.Yams.Client
{
    public class YamsClientFactory : IYamsClientFactory
    {
        public IYamsClient CreateYamsClient(YamsClientConfig config)
        {
            var options = new YamsClientOptions();
            bool isValid = Parser.Default.ParseArgumentsStrict(config.ProcessArgs, options);

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