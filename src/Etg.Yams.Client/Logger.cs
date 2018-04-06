namespace Etg.Yams.Client
{
    static class Logger
    {
        public static string FormatMessage(this string message, YamsClientOptions options)
        {
            return $"[{options.AppName} ({options.AppVersion})] {message}";
        }
    }
}
