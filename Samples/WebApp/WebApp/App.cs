using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace WebApp
{
    public class App
    {
        public static string Id;
        public static string Version;
        public static string ClusterId;

        static void Main(string[] args)
        {
            Id = args[0];
            Version = args[1];
            ClusterId = args[2];

            Version version = new Version(Version);
            string apiVersion = string.Format("{0}.{1}", version.Major, version.Minor);
            string baseUrl = string.Format("http://{0}/{1}/{2}", GetIpAddress(), Id, apiVersion);
            Console.WriteLine("Url is: " + baseUrl);

            // Start OWIN host 
            Microsoft.Owin.Hosting.WebApp.Start<Startup>(url: baseUrl);
            Console.WriteLine("WebApp has been started successfully");
            Console.ReadLine(); 

        }

        private static string GetIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork).ToString();
        }
    }
}
