namespace Etg.Yams.Configuration
{
    public class WithConfig : IWithConfig
    {
        public WithConfig(YamsConfig config)
        {
            this.Config = config;
        }

        public YamsConfig Config { get; }
    }
}
