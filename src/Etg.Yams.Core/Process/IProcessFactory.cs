using Etg.Yams.Application;

namespace Etg.Yams.Process
{
    public interface IProcessFactory
    {
        IProcess CreateProcess(string exePath, bool monitorInitialization, bool monitorHealth, bool gracefulShutdown);
    }
}
