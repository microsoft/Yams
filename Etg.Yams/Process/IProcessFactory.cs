namespace Etg.Yams.Process
{
    public interface IProcessFactory
    {
        IProcess CreateProcess(string exePath, string exeArgs);
    }
}
