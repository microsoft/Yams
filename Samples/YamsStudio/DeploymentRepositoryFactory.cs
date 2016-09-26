// -------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// -------------------------------------------------------------------

using Etg.Yams.Azure.Storage;
using Etg.Yams.Json;
using Etg.Yams.Storage;
using Etg.Yams.Storage.Config;
using Newtonsoft.Json.Serialization;

namespace YamsStudio
{
    public class DeploymentRepositoryFactory : IDeploymentRepositoryFactory
    {
        public IDeploymentRepository CreateRepository(string connectionString)
        {
            return new BlobStorageDeploymentRepository(connectionString, new JsonDeploymentConfigSerializer(
                new JsonSerializer(new DiagnosticsTraceWriter())));
        }
    }
}
