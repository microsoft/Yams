using Etg.Yams.Storage;
using Etg.Yams.Update;

namespace Etg.Yams.Configuration
{
    public class WithDeploymentRepository : IWithDeploymentRepository
    {
        public WithDeploymentRepository(YamsConfig config, IUpdateSessionManager updateSessionManager, IDeploymentRepository deploymentRepository, IDeploymentStatusWriter deploymentStatusWriter)
        {
            this.Config = config;
            this.UpdateSessionManager = updateSessionManager;
            this.DeploymentRepository = deploymentRepository;
            this.DeploymentStatusWriter = deploymentStatusWriter;
        }

        public YamsConfig Config { get; }
        public IUpdateSessionManager UpdateSessionManager { get; }
        public IDeploymentRepository DeploymentRepository { get; }
        public IDeploymentStatusWriter DeploymentStatusWriter { get; }

        public IYamsService Build()
        {
            return new YamsDiModule(this.Config, this.DeploymentRepository, this.DeploymentStatusWriter, this.UpdateSessionManager).YamsService;
        }
    }
}
