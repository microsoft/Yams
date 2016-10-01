using System.Collections.Generic;
using System.Linq;
using Etg.Yams.Application;
using Etg.Yams.Storage.Config;
using Etg.Yams.Utils;

namespace Etg.Yams.Install
{
    public class AppInstallConfig
    {
        public AppInstallConfig(AppIdentity appIdentity, IReadOnlyDictionary<string, string> properties) : this(appIdentity)
        {
            Properties = properties.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public AppInstallConfig(AppIdentity appIdentity)
        {
            AppIdentity = appIdentity;
            Properties = new Dictionary<string, string>();
        }

        protected bool Equals(AppDeploymentConfig other)
        {
            bool res = Equals(AppIdentity, other.AppIdentity)
                       && Properties.SequenceEqual(other.Properties);
            return res;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AppDeploymentConfig)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (AppIdentity != null ? AppIdentity.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Properties != null ? HashCodeUtils.GetHashCode(Properties) : 0);
                return hashCode;
            }
        }

        public AppIdentity AppIdentity { get; }

        public IReadOnlyDictionary<string, string> Properties { get; }
    }
}