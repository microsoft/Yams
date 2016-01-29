namespace Etg.Yams.Process
{
    public class SelfRestartingProcessFactory : IProcessFactory
    {
        private readonly int _maximumRestartAttempts;

        public SelfRestartingProcessFactory(int maximumRestartAttempts)
        {
            _maximumRestartAttempts = maximumRestartAttempts;
        }

        public IProcess CreateProcess(string exePath, string args)
        {
            IProcess process = new Process(exePath, args);
            return new SelfRestartingProcess(process, _maximumRestartAttempts);
        }
    }
}