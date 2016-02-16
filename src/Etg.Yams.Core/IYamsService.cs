using System.Threading.Tasks;

namespace Etg.Yams
{
    public interface IYamsService
    {
        Task Start();
        Task Stop();
    }
}