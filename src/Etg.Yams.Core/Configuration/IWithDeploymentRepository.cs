namespace Etg.Yams.Configuration
{
    public interface IWithDeploymentRepository : IFluentInterface
    {
        IYamsService Build();
    }
}
