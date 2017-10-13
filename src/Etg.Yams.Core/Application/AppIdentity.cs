using System;
using Semver;
using Newtonsoft.Json;

namespace Etg.Yams.Application
{
    public class AppIdentity : IEquatable<AppIdentity>
    {
        private readonly string _id;
        private readonly SemVersion _version;

        /// <summary>
        /// A unique identifier for an application represented by an <see cref="Id"/> and a <see cref="Version"/>/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="version"></param>
        public AppIdentity(string id, SemVersion version)
        {
            _id = id;
            _version = version;
        }

        [JsonConstructor]
        public AppIdentity(string id, string version)
        {
            _id = id;
            _version = SemVersion.Parse(version);
        }

        public override string ToString()
        {
            return string.Format("Id: {0}, Version: {1}", Id, Version);
        }

        public string Id
        {
            get { return _id; }
        }

        public SemVersion Version
        {
            get { return _version; }
        }

        public bool Equals(AppIdentity other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(_id, other._id) && Equals(_version, other._version);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AppIdentity)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_id != null ? _id.GetHashCode() : 0) * 397) ^ (_version != null ? _version.GetHashCode() : 0);
            }
        }

        public static bool operator ==(AppIdentity left, AppIdentity right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AppIdentity left, AppIdentity right)
        {
            return !Equals(left, right);
        }
    }
}
