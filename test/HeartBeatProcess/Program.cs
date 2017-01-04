using Etg.Yams.Client;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace HeartBeatProcess
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Run(args).Wait();
        }

        private static async Task Run(string[] args)
        {
            Console.WriteLine("args " + string.Join(" ", args));
            int heartBeatPeriod = 1000 * Convert.ToInt32(args[2]);

            var yamsClientConfig = new YamsClientConfigBuilder(args).Build();
            var yamsClientFactory = new YamsClientFactory();

            Console.WriteLine("Initializing...");
            IYamsClient yamsClient = yamsClientFactory.CreateYamsClient(yamsClientConfig);

            await yamsClient.Connect();

            File.WriteAllText($"HeartBeatApp.exe.out", $"HeartBeatApp.exe {args[0]} {args[1]}");

            while (true)
            {
                await Task.Delay(heartBeatPeriod);
                Console.WriteLine("Sending heart beat..");
                await yamsClient.SendHeartBeat();
                Console.WriteLine("Heart beat sent!");
            }
        }
    }
}