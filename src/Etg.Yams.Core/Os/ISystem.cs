using System;

namespace Etg.Yams.Os
{
    /// <summary>
    /// Interface to interact with the operating system. Add apis as needed.
    /// </summary>
    public interface ISystem
    {
        string GetEnvironmentVariable(string name, EnvironmentVariableTarget target);
    }
}
