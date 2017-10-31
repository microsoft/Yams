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
                throw new ArgumentException(
                    $"Cannot add application {application.Identity} to the application pool because it is already there");
            }

            if (! await StartApplication(application))
            {
                throw new Exception($"Failed to start application {application.Identity}");
            }

            if (!_applications.TryAdd(application.Identity, application))
            {
                throw new Exception(
                    $"Could not add the application {application.Identity} to the concurent dictionary. This is likely a bug");
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

        public IEnumerable<IApplication> Applications => _applications.Values;

        public async Task RemoveApplication(AppIdentity appIdentity)
        {
            IApplication application;
            if (!_applications.TryRemove(appIdentity, out application))
            {
                throw new ArgumentException(
                    $"Cannot remove application {appIdentity} because it doesn't exist in the pool");
            }
            application.Exited -= OnApplicationExited;
            try
            {
                await application.Stop();
                application.Dispose();
                await Task.Delay(5000);
            }
            catch (Exception e)
            {
                Trace.TraceError($"Exception occured while stopping Application {appIdentity}, Exception: {e}");
            }
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
            Trace.TraceError($"Application {appIdentity} exited unexpectedly with message {args.Message}");
            if (_applications.ContainsKey(appIdentity))
            {
                RemoveApplication(appIdentity).GetAwaiter().GetResult();
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
            var tasks = new List<Task>();
            foreach (IApplication application in Applications)
            {
                tasks.Add(RemoveApplication(application.Identity));
                application.Dispose();
            }
            Task.WhenAll(tasks).Wait();
        }
    }
}
