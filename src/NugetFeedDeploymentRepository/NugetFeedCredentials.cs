namespace Etg.Yams.NuGet.Storage
{
    public class NugetFeedCredentials
    {
        public NugetFeedCredentials(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }

        public string Username { get; }
        public string Password { get; }
    }
}
