namespace YamsStudio
{
    public class StorageAccountConnectionInfo
    {
	    public StorageAccountConnectionInfo(string accountName, string connectionString)
        {
            AccountName = accountName;
            ConnectionString = connectionString;
        }

        public string AccountName { get; }

	    public string ConnectionString { get; }
    }
}