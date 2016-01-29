using System;
using System.IO;
using System.Reflection;

namespace TestProcess
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().Run(args);
        }

        private void Run(string[] args)
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            string exeName = Path.GetFileName(codeBase);

            File.WriteAllText(string.Format("{0}.out", exeName), string.Format("{0} ", exeName) + string.Join(" ", args));

            Console.ReadLine();
        }
    }
}
