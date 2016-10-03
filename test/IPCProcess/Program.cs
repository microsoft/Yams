using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;

namespace IPCProcess
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.Sleep(TimeSpan.FromSeconds(30));
            if (args.Length > 0)
            {
                using (PipeStream pipeClient =
                    new AnonymousPipeClientStream(PipeDirection.Out, args.Last()))
                {
                    try
                    {
                        // Read user input and send that to the client process.
                        using (StreamWriter sw = new StreamWriter(pipeClient))
                        {
                            sw.AutoFlush = true;
                            // Send a 'sync message' and wait for client to receive it.
                            sw.WriteLine("[STARTED]");
                            pipeClient.WaitForPipeDrain();
                        }
                    }
                    // Catch the IOException that is raised if the pipe is broken
                    // or disconnected.
                    catch (IOException e)
                    {
                        Console.WriteLine("App Error: {0}", e.Message);
                    }
                }
            }
            Console.ReadLine();
        }
    }
}
