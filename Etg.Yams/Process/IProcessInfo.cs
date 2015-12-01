namespace Etg.Yams.Process
{
    public interface IProcessInfo
    {
        string ExePath { get; }
        string ExeArgs { get; }
    }
}