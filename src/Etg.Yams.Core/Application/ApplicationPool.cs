using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Etg.Yams.Application
{
    public class ApplicationPool : IApplicationPool
    {
        private readonly ConcurrentDictionary<AppIdentity, IApplication> _applications;

        public ApplicationPool()
        {
            _applications = new ConcurrentDictionary<AppIdentity, IApplication>();
        }

        public async Task AddApplication(IApplication application)
        {
            if (HasApplication(application.Identity))
            {
                throw new ArgumentException(string.Format("Cannot add application {0} to the application pool because it is already there", application.Identity));    
            }

            if (! await StartApplication(application))
            {
                throw new Exception(string.Format("Failed to start application {0}", application.Identity));
            }

            if (!_applications.TryAdd(application.Identity, application))
            {
                throw new Exception(string.Format("Could not add the application {0} to the concurent dictionary. This is likely a bug", application.Identity));
            }
            application.Exited += OnApplicationExited;
        }

        public bool HasApplication(AppIdentity appIdentity)
        {
            return _applications.ContainsKey(appIdentity);
        }

        public IApplication GetApplication(AppIdentity appIdentity)
        {
            if (!HasApplication(appIdentity))
            {
                return null;
            }
            return _applications[appIdentity];
        }

        public IEnumerable<IApplication> Applications {
            get { return _applications.Values; }
        }

        public async Task RemoveApplication(AppIdentity appIdentity)
        {
            IApplication application;
            if (!_applications.TryRemove(appIdentity, out application))
            {
                throw new ArgumentException(string.Format("Cannot remove application {0} because it doesn't exist in the pool", appIdentity));
            }
            application.Exited -= OnApplicationExited;
            await application.Stop();
            await Task.Delay(5000);
        }

        private static async Task<bool> StartApplication(IApplication application)
        {
            if (!await application.Start())
            {
                Trace.TraceError("Could not start application {0}", application.Identity);
                return false;
            }
            Trace.TraceInformation("Successfully started application {0}", application.Identity);
            return true;
        }

        private void OnApplicationExited(object sender, ApplicationExitedArgs args)
        {
            AppIdentity appIdentity = args.AppIdentity;
            Trace.TraceError("Application {0} exited unexpectedly with message {1}", appIdentity, args.Message);
            if (_applications.ContainsKey(appIdentity))
            {
                RemoveApplication(appIdentity).Wait();
            }
        }

        public async Task Shutdown()
        {
            foreach (var application in _applications.Values)
            {
                await application.Stop();
            }
            _applications.Clear();
        }

        public void Dispose()
        {
            foreach (IApplication application in Applications)
            {
                application.Dispose();
            }
        }
    }
}
