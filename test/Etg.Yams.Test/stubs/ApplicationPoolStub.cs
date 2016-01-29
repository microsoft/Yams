using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Etg.Yams.Application;

namespace Etg.Yams.Test.stubs
{
    public class ApplicationPoolStub : IApplicationPool
    {
        private readonly Dictionary<AppIdentity, IApplication> _applications;
        private readonly HashSet<AppIdentity> _addedApplications;

        public ApplicationPoolStub()
        {
            _applications = new Dictionary<AppIdentity, IApplication>();    
            _addedApplications = new HashSet<AppIdentity>();
        }

        public Task AddApplication(IApplication application)
        {
            if (HasApplication(application.Identity))
            {
                throw new Exception("Attempt to add an existing application");
            }
            _applications[application.Identity] = application;
            _addedApplications.Add(application.Identity);
            return Task.FromResult(true);
        }

        public Task RemoveApplication(AppIdentity appIdentity)
        {
            _applications.Remove(appIdentity);
            return Task.FromResult(true);
        }

        public bool HasApplication(AppIdentity appIdentity)
        {
            return _applications.ContainsKey(appIdentity);
        }

        public IApplication GetApplication(AppIdentity appIdentity)
        {
            return _applications[appIdentity];
        }

        public IEnumerable<IApplication> Applications
        {
            get { return _applications.Values; }
        }

        public Task Shutdown()
        {
            return Task.FromResult(true);
        }

        public bool HasApplicationBeenAdded(AppIdentity appIdentity)
        {
            return _addedApplications.Contains(appIdentity);
        }

        public void Dispose()
        {
        }
    }
}
