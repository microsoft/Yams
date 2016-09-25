using System.Linq;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Deploy
{
    public class AndDeploymentMatcher : IAppDeploymentMatcher
    {
        private readonly IAppDeploymentMatcher[] _matchers;

        public AndDeploymentMatcher(params IAppDeploymentMatcher[] matchers)
        {
            _matchers = matchers;
        }

        public bool IsMatch(AppDeploymentConfig appDeploymentConfig)
        {
            return _matchers.All(matcher => matcher.IsMatch(appDeploymentConfig));
        }
    }
}