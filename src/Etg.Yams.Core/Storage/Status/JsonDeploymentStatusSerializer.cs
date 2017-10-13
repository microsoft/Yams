using Etg.Yams.Json;
using System.Collections.Generic;

namespace Etg.Yams.Storage.Status
{
    public class JsonDeploymentStatusSerializer : IDeploymentStatusSerializer
    {
        private readonly IJsonSerializer _jsonSerializer;

        public JsonDeploymentStatusSerializer(IJsonSerializer jsonSerializer)
        {
            _jsonSerializer = jsonSerializer;
        }

        public InstanceDeploymentStatus Deserialize(string data)
        {
            if (data == null)
            {
                return new InstanceDeploymentStatus();
            }
            return _jsonSerializer.Deserialize<InstanceDeploymentStatus>(data);
        }

        public string Serialize(InstanceDeploymentStatus instanceDeploymentStatus)
        {
            return _jsonSerializer.Serialize(instanceDeploymentStatus);
        }
    }
}
