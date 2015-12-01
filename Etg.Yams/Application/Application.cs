using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Etg.Yams.Process;

namespace Etg.Yams.Application
{
    public abstract class Application : IApplication
    {
        protected Application(AppIdentity identity, string path)
        {
            Identity = identity;
            Path = path;
        }

        protected async Task<bool> StartProcess(IProcess process)
        {
            try
            {
                await process.Start();
                process.Exited += OnProcessExited;
                return true;
            }
            catch (Exception)
            {
                Trace.TraceInformation("Could not start the host process for application {0}", Identity);
                return false;    
            }
        }

        protected void OnProcessExited(object sender, ProcessExitedArgs e)
        {
            var handler = Exited;
            if (handler != null)
            {
                Trace.TraceInformation("The process for application {0} has exited unexpectedly", Identity);
                handler(this, new ApplicationExitedArgs { Message = e.Message, AppIdentity = Identity});
            }
        }

        public AppIdentity Identity { get; private set; }

        public string Path { get; private set; }

        public abstract Task<bool> Start();
        public abstract Task Stop();

        public event EventHandler<ApplicationExitedArgs> Exited;
        public abstract void Dispose();
    }
}