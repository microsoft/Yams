using System;
using System.Net;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;

namespace SiloHost
{
    /// <summary>
    /// Orleans test silo host
    /// </summary>
    public class Program
    {
        private static OrleansHostWrapper hostWrapper;
        static int Main(string[] args)
        {
            int exitCode = StartSilo(args);

            Console.WriteLine("Orleans Silo is running.\nPress Enter to terminate...");
            Console.ReadLine();

            exitCode += ShutdownSilo();

            //either StartSilo or ShutdownSilo failed would result on a non-zero exit code. 
            return exitCode;
        }

        private static int StartSilo(string[] args)
        {
            // define the cluster configuration
            var config = new ClusterConfiguration();
            config.Globals.LivenessType = GlobalConfiguration.LivenessProviderType.AzureTable;
            config.Globals.ReminderServiceType = GlobalConfiguration.ReminderServiceProviderType.AzureTable;
            config.Globals.DataConnectionString = "MY_DATA_CONNECTION_STRING";
            config.AddMemoryStorageProvider();
            config.AddAzureTableStorageProvider("AzureStore");
            config.Defaults.DefaultTraceLevel = Severity.Error;
            config.Defaults.Port = 100;
            config.Defaults.ProxyGatewayEndpoint = new IPEndPoint(config.Defaults.Endpoint.Address, 101);

            hostWrapper = new OrleansHostWrapper(config, args);
            return hostWrapper.Run();
        }

        private static int ShutdownSilo()
        {
            if (hostWrapper != null)
            {
                return hostWrapper.Stop();
            }
            return 0;
        }
    }
}
