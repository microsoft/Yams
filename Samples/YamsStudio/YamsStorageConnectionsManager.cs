using System.Collections.Concurrent;
using System.Collections.Generic;
using Etg.Yams.Storage;

namespace YamsStudio
{
    public class YamsStorageConnectionsManager
    {
        private readonly IYamsRepositoryFactory _connectionFactory;
        private readonly Dictionary<string, IYamsRepository> _connections 
            = new Dictionary<string, IYamsRepository>();
        public YamsStorageConnectionsManager(IYamsRepositoryFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IYamsRepository GetConnection(StorageAccountConnectionInfo connectionInfo)
        {
            string connectionString = connectionInfo.ConnectionString;
            if (!_connections.ContainsKey(connectionString))
            {
                IYamsRepository connection = _connectionFactory.CreateRepository(connectionInfo.ConnectionString);
                _connections.Add(connectionString, connection);
            }

            return _connections[connectionString];
        }
    }
}