using System.Threading.Tasks;

namespace Etg.Yams.Process
{
    public interface IProcessStopper
    {
        Task StopProcess(IProcess process);
    }
}
