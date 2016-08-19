using System;

namespace SiloHost
{
    /// <summary>
    /// Orleans test silo host
    /// </summary>
    public class Program
    {
        static int Main(string[] args)
        {
            int returnCode = StartSilo(args);

            Console.WriteLine("Orleans Silo is running.\nPress Enter to terminate...");
            Console.ReadLine();

            returnCode += ShutdownSilo();
            return returnCode; // either StartSilo or ShutdownSilo failing would result in a non-zero return code.
        }

        private static int StartSilo(string[] args)
        {
            hostWrapper = new OrleansHostWrapper(args);

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

        private static OrleansHostWrapper hostWrapper;
    }
}
