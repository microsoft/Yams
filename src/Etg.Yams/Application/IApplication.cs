using System;
using System.Threading.Tasks;

namespace Etg.Yams.Application
{
    /// <summary>
    /// Represents an application that will be deployed and hosted in YAMS. An application is uniquely identified by
    /// an id and a version.
    /// </summary>
    public interface IApplication : IDisposable
    {
        AppIdentity Identity { get; }

        /// <summary>
        /// The path where the application has been installed.
        /// </summary>
        string Path { get; }

        Task<bool> Start();

        Task Stop();

        /// <summary>
        /// Is triggered if the application exited before <see cref="Stop"/> is called. The event will not be emitted 
        /// when <see cref="Stop"/> is called.
        /// </summary>
        event EventHandler<ApplicationExitedArgs> Exited;
    }

    public class ApplicationExitedArgs : EventArgs
    {
        public AppIdentity AppIdentity { get; set; }

        public string Message { get; set; }
    }
}
