#region

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Etg.Yams.Client;

#endregion

namespace WebApp
{
    public class App
    {
        public static string Id;
        public static string Version;
        public static string ClusterId;
        public static string InstanceId;

        private static readonly TimeSpan HeartBeatPeriod = TimeSpan.FromSeconds(5);

        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        private static async Task MainAsync(string[] args)
        {
            Id = args[0];
            Version = args[1];
            ClusterId = args[2];
            InstanceId = args[3];

            Version version = new Version(Version);
            string apiVersion = $"{version}";
            string baseUrl = $"http://{GetIpAddress()}:8008/{Id}/{apiVersion}";
            Console.WriteLine("Url is: " + baseUrl);

            // Start OWIN host 
            Microsoft.Owin.Hosting.WebApp.Start<Startup>(url: baseUrl);
            Console.WriteLine("WebApp has been started successfully");
            
            var yamsClientConfig = new YamsClientConfigBuilder(args).Build();
            var yamsClientFactory = new YamsClientFactory();
            IYamsClient yamsClient = yamsClientFactory.CreateYamsClient(yamsClientConfig);

            await yamsClient.Connect();
            await yamsClient.SendInitializationDoneMessage();

            var exitMessageReceived = false;
            yamsClient.ExitMessageReceived += (sender, eventArgs) => { exitMessageReceived = true; };

            while (!exitMessageReceived)
            {
                await Task.Delay(HeartBeatPeriod);
                await yamsClient.SendHeartBeat();
            }
        }

        private static string GetIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork).ToString();
        }
    }
}