using System;
using System.Threading.Tasks;
using Etg.Yams.Application;

namespace Etg.Yams.Test.stubs
{
    public class ApplicationStub : IApplication
    {
        private bool _running;

        public AppIdentity Identity { get; private set; }
        public string Path { get; private set; }

        public ApplicationStub(AppIdentity appIdentity, string path)
        {
            Identity = appIdentity;
            Path = path;
        }

        public Task<bool> Start()
        {
            _running = true;
            return Task.FromResult(true);
        }

        public Task Stop()
        {
            _running = false;
            return Task.FromResult(true);
        }

        public bool IsRunning()
        {
            return _running;
        }

        public void Fail()
        {
            if (Exited != null) Exited(this, new ApplicationExitedArgs{AppIdentity = Identity});
        }

        public event EventHandler<ApplicationExitedArgs> Exited;
        public void Dispose()
        {
        }
    }
}
