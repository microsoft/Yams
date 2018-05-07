using System;

namespace Etg.Yams.Os
{
    public class System : ISystem
    {
        public string GetEnvironmentVariable(string name, EnvironmentVariableTarget target)
        {
            return Environment.ExpandEnvironmentVariables(Environment.GetEnvironmentVariable(name, target));
        }
    }
}
