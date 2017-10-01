using System.Collections.Generic;
using System.Linq;
using Etg.Yams.Storage.Config;

namespace Etg.Yams.Deploy
{
    public class PropertiesDeploymentMatcher : IAppDeploymentMatcher
    {
        private readonly IReadOnlyDictionary<string, string> _matchProperties; 
        public PropertiesDeploymentMatcher(IReadOnlyDictionary<string, string> matchProperties)
        {
            _matchProperties = matchProperties.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public bool IsMatch(AppDeploymentConfig appDeploymentConfig)
        {
            foreach (var kvp in _matchProperties)
            {
                string value;
                if (appDeploymentConfig.Properties.TryGetValue(kvp.Key, out value))
                {
                    if (value != kvp.Value)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}