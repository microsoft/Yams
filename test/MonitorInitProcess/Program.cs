using Etg.Yams.Client;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MonitorInitProcess
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
            var yamsClientConfig = new YamsClientConfigBuilder(args).Build();
            var yamsClientFactory = new YamsClientFactory();

            Console.WriteLine("Initializing...");
            IYamsClient yamsClient = yamsClientFactory.CreateYamsClient(yamsClientConfig);

            Task initTimeMax = Task.Delay(1000*Convert.ToInt32(args[2]));

            File.WriteAllText($"MonitorInitApp.exe.out", $"MonitorInitApp.exe {args[0]} {args[1]}");

            await Task.WhenAll(yamsClient.Connect(), Task.Delay(TimeSpan.FromSeconds(5)), initTimeMax);

            Console.WriteLine("Send initialization done message...");
            await yamsClient.SendInitializationDoneMessage();
            Console.WriteLine("Initialization done message sent!");

            while (true)
            {
                await Task.Delay(1000);
                Console.WriteLine("Doing work");
            }
        }
    }
}