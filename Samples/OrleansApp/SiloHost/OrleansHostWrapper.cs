using System;
using System.Net;
using Orleans.Runtime.Configuration;
using Orleans.Runtime;

namespace SiloHost
{
    class OrleansHostWrapper
    {
        private Orleans.Runtime.Host.SiloHost siloHost;

        public OrleansHostWrapper(string[] args)
        {
            SiloArgs siloArgs = ParseArguments(args);
            if (siloArgs != null)
            {
                Init(siloArgs);
            }
        }

        public int Run()
        {
            // if siloHost is not constructed yet due to wrong cmd param
            if (siloHost == null)
            {
                Console.Error.WriteLine("Failed to initialize Orleans silo due to bad command line arguments");
                return 1;
            }
            else
            {
                try
                {
                    siloHost.InitializeOrleansSilo();

                    if (siloHost.StartOrleansSilo())
                    {
                        Console.WriteLine(string.Format("Successfully started Orleans silo '{0}' as a {1} node.", siloHost.Name, siloHost.Type));
                        return 0;
                    }
                    else
                    {
                        throw new OrleansException(string.Format("Failed to start Orleans silo '{0}' as a {1} node.", siloHost.Name, siloHost.Type));
                    }
                }
                catch (Exception exc)
                {
                    siloHost.ReportStartupError(exc);
                    var msg = string.Format("{0}:\n{1}\n{2}", exc.GetType().FullName, exc.Message, exc.StackTrace);
                    Console.WriteLine(msg);
                    return 1;
                }
            }
        }


        public int Stop()
        {
            if (siloHost != null)
            {
                try

                {
                    siloHost.StopOrleansSilo();
                    siloHost.Dispose();
                    Console.WriteLine(string.Format("Orleans silo '{0}' shutdown.", siloHost.Name));
                }
                catch (Exception exc)
                {
                    siloHost.ReportStartupError(exc);
                    var msg = string.Format("{0}:\n{1}\n{2}", exc.GetType().FullName, exc.Message, exc.StackTrace);
                    Console.WriteLine(msg);
                    return 1;
                }
            }
            return 0;
        }

        private void Init(SiloArgs siloArgs)
        {
            var config = new ClusterConfiguration();
            config.Globals.LivenessType = GlobalConfiguration.LivenessProviderType.AzureTable;
            config.Globals.ReminderServiceType = GlobalConfiguration.ReminderServiceProviderType.AzureTable;
            config.Globals.DataConnectionString = "MY_DATA_CONNECTION_STRING";
            config.AddMemoryStorageProvider();
            config.AddAzureTableStorageProvider("AzureStore");
            config.Globals.DeploymentId = siloArgs.DeploymentId;
            config.Defaults.DefaultTraceLevel = Severity.Error;
            config.Defaults.Port = 100;
            config.Defaults.ProxyGatewayEndpoint = new IPEndPoint(config.Defaults.Endpoint.Address, 101);
            siloHost = new Orleans.Runtime.Host.SiloHost(siloArgs.SiloName, config);
            siloHost.LoadOrleansConfig();
        }

        private SiloArgs ParseArguments(string[] args)
        {
            string deploymentId = null;
            string siloName = null;
            
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith("-") || arg.StartsWith("/"))
                {
                    switch (arg.ToLowerInvariant())
                    {
                        case "/?":
                        case "/help":
                        case "-?":
                        case "-help":
                            // Query usage help
                            PrintUsage();
                            return null;
                        default:
                            Console.WriteLine("Bad command line arguments supplied: " + arg);
                            return null;
                    }
                }
                else if (arg.Contains("="))
                {
                    string[] parameters = arg.Split('=');
                    if (String.IsNullOrEmpty(parameters[1]))
                    {
                        Console.WriteLine("Bad command line arguments supplied: " + arg);
                        return null;
                    }
                    switch (parameters[0].ToLowerInvariant())
                    {
                        case "deploymentid":
                            deploymentId = parameters[1];
                            break;
                        case "name":
                            siloName = parameters[1];
                            break;
                        default:
                            Console.WriteLine("Bad command line arguments supplied: " + arg);
                            return null;
                    }
                }
                else
                {
                    Console.WriteLine("Bad command line arguments supplied: " + arg);
                    return null;
                }
            }
            
            // Default to machine name
            siloName = siloName ?? Dns.GetHostName();
            return new SiloArgs(siloName, deploymentId);
        }

        public void PrintUsage()
        {
            Console.WriteLine(
@"USAGE: 
    orleans host [<siloName> [<configFile>]] [DeploymentId=<idString>] [/debug]
Where:
    <siloName>      - Name of this silo in the Config file list (optional)
    DeploymentId=<idString> 
                    - Which deployment group this host instance should run in (optional)");
        }
    }

    class SiloArgs
    {
        public SiloArgs(string siloName, string deploymentId)
        {
            this.DeploymentId = deploymentId;
            this.SiloName = siloName;
        }

        public SiloArgs()
        {
            
        }

        public string SiloName { get; set; }
        public string DeploymentId { get; set; }
    }
}
