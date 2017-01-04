using System;
using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Client;

namespace FullIpcProcess
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

            IYamsClient yamsClient = yamsClientFactory.CreateYamsClient(yamsClientConfig);

            await Task.WhenAll(yamsClient.Connect(), Initialize());

            bool exitMessageReceived = false;
            yamsClient.ExitMessageReceived += (sender, eventArgs) =>
            {
                Console.WriteLine("Exit message received!");
                exitMessageReceived = true;
            };

            File.WriteAllText($"FullIpcApp.exe.out", $"FullIpcApp.exe {args[0]} {args[1]}");

            Console.WriteLine("Send initialization done message...");
            await yamsClient.SendInitializationDoneMessage();
            Console.WriteLine("Initialization done message sent!");

            while (!exitMessageReceived)
            {
                await DoWork();
                Console.WriteLine("Sending heart beat..");
                await yamsClient.SendHeartBeat();
                Console.WriteLine("Heart beat sent!");
            }
            await Shutdown();
            Console.WriteLine("Exiting..");
        }

        private static async Task Shutdown()
        {
            await Task.Delay(1000);
        }

        private static async Task DoWork()
        {
            Console.WriteLine("Doing work");
            await Task.Delay(1000);
        }

        private static Task Initialize()
        {
            return Task.Delay(1000);
        }
    }
}