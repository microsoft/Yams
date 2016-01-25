// -------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// -------------------------------------------------------------------

using Etg.Yams.Storage;

namespace YamsStudio
{
    public class DeploymentRepositoryFactory : IDeploymentRepositoryFactory
    {
        public IDeploymentRepository CreateRepository(string connectionString)
        {
            return new BlobStorageDeploymentRepository(connectionString);
        }
    }
}
