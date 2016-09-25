using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Etg.Yams.Json
{
    public class JsonSerializer : IJsonSerializer
    {
        private readonly ITraceWriter _traceWriter;

        public JsonSerializer(ITraceWriter traceWriter)
        {
            _traceWriter = traceWriter;
        }

        public Task<T> DeserializeAsync<T>(string data)
        {
            return Task.Run(() => Deserialize<T>(data));
        }

        public Task SerializeAsync(object data)
        {
            return Task.Run(() => Serialize(data));
        }

        public T Deserialize<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data, new JsonSerializerSettings
            {
                TraceWriter = _traceWriter
            });
        }

        public string Serialize(object data)
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented, new JsonSerializerSettings
            {
                TraceWriter = _traceWriter
            });
        }
    }
}