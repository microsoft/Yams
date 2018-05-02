using Etg.Yams.Configuration;

namespace Etg.Yams
{
    public static class YamsServiceBuilder
    {
        public static IWithConfig WithConfig(YamsConfig config)
        {
            return new WithConfig(config);
        }
    }
}
