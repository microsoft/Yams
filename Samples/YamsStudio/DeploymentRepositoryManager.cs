using System.Collections.Generic;
using Etg.Yams.Storage;

namespace YamsStudio
{
    public class DeploymentRepositoryManager
    {
        private readonly IDeploymentRepositoryFactory _connectionFactory;
        private readonly Dictionary<string, IDeploymentRepository> _repos 
            = new Dictionary<string, IDeploymentRepository>();
        public DeploymentRepositoryManager(IDeploymentRepositoryFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IDeploymentRepository GetRepository(StorageAccountConnectionInfo connectionInfo)
        {
            string connectionString = connectionInfo.ConnectionString;
            if (!_repos.ContainsKey(connectionString))
            {
                IDeploymentRepository repository = _connectionFactory.CreateRepository(connectionInfo.ConnectionString);
                _repos.Add(connectionString, repository);
            }

            return _repos[connectionString];
        }
    }
}