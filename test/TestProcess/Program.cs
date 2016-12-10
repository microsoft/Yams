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

            File.WriteAllText($"{exeName}.out", $"{exeName} " + string.Join(" ", args));

            Console.ReadLine();
        }
    }
}
