using Etg.Yams.Configuration;
using System;
using Etg.Yams.Update;
using Etg.Yams.Azure.UpdateSession;

namespace Etg.Yams
{
    public static class AzureTableUpdateSessionManagerConfigurator
    {
        public static IWithUpdateSessionManager UsingAzureTableUpdateSessionManager(this IWithConfig builder, string connectionString)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (builder is WithConfig x)
            {
                IUpdateSessionManager updateSessionManager = new AzureStorageUpdateSessionDiModule(
                    x.Config.SuperClusterId,
                    x.Config.ClusterId,
                    x.Config.InstanceId,
                    x.Config.InstanceUpdateDomain,
                    connectionString,
                    x.Config.UpdateSessionTtl).UpdateSessionManager;

                return new WithUpdateSessionManager(x.Config, updateSessionManager);
            }

            throw new NotSupportedException($"Expecting {typeof(IWithConfig).FullName} to be implemented by {typeof(WithConfig).FullName}. Instead received an instance of {builder.GetType().FullName}.");
        }
    }
}
