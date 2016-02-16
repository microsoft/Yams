namespace Etg.Yams.Process
{
    public class SelfRestartingProcessFactory : IProcessFactory
    {
        private readonly int _maximumRestartAttempts;
        private readonly bool _showProcessWindow;

        public SelfRestartingProcessFactory(int maximumRestartAttempts, bool showProcessWindow)
        {
            _maximumRestartAttempts = maximumRestartAttempts;
            _showProcessWindow = showProcessWindow;
        }

        public IProcess CreateProcess(string exePath, string args)
        {
            IProcess process = new Process(exePath, args, _showProcessWindow);
            return new SelfRestartingProcess(process, _maximumRestartAttempts);
        }
    }
}