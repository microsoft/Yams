using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Orleans;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;

namespace WebApp
{
    public class App
    {
        public static string Id;
        public static string Version;
        public static string DeploymentId;

        static void Main(string[] args)
        {
            Id = args[0];
            Version = args[1];
            DeploymentId = args[2];

            Version version = new Version(Version);
            string apiVersion = string.Format("{0}.{1}", version.Major, version.Minor);
            string baseUrl = string.Format("http://{0}/{1}/{2}", GetIpAddress(), Id, apiVersion);
            Console.WriteLine("Url is: " + baseUrl);

            // Start OWIN host 
            Microsoft.Owin.Hosting.WebApp.Start<Startup>(url: baseUrl);
            Console.WriteLine("WebApp has been started successfully");

            var config = new ClientConfiguration();
            config.GatewayProvider = ClientConfiguration.GatewayProviderType.AzureTable;
            config.DeploymentId = "hello.orleans_1.0_MY_YAMS_BACKEND_CLUSTER_ID";
            config.DataConnectionString = "MY_DATA_CONNECTION_STRING";
            config.DefaultTraceLevel = Severity.Error;

            // Attempt to connect a few times to overcome transient failures and to give the silo enough 
            // time to start up when starting at the same time as the client (useful when deploying or during development).

            const int initializeAttemptsBeforeFailing = 5;

            int attempt = 0;
            while (true)
            {
                try
                {
                    GrainClient.Initialize(config);
                    Console.WriteLine("Client initialized");
                    break;
                }
                catch (SiloUnavailableException e)
                {
                    attempt++;
                    if (attempt >= initializeAttemptsBeforeFailing)
                    {
                        throw;
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }
            }

            Console.ReadLine(); 

        }

        private static string GetIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork).ToString();
        }
    }
}
