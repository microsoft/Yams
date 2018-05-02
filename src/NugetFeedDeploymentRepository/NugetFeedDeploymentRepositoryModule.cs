using Autofac;

namespace Etg.Yams.NuGet.Storage
{
    class NugetFeedDeploymentRepositoryModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<NugetPackageExtractor>().AsImplementedInterfaces();
        }
    }
}
