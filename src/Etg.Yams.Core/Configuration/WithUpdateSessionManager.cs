using Etg.Yams.Update;

namespace Etg.Yams.Configuration
{
    public class WithUpdateSessionManager : IWithUpdateSessionManager
    {
        public WithUpdateSessionManager(YamsConfig config, IUpdateSessionManager updateSessionManager)
        {
            this.Config = config;
            this.UpdateSessionManager = updateSessionManager;
        }

        public YamsConfig Config { get; }
        public IUpdateSessionManager UpdateSessionManager { get; }
    }
}
