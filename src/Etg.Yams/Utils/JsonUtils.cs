using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Etg.Yams.Utils
{
    public static class JsonUtils
    {
        public static async Task<T> ParseFile<T>(string path)
        {
            using (StreamReader r = new StreamReader(path))
            {
                return await DeserializeAsync<T>(await r.ReadToEndAsync());
            }
        }

        public static Task<T> DeserializeAsync<T>(string data)
        {
            return Task.Run(() => Deserialize<T>(data));
        }

        public static T Deserialize<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data);
        }

        public static string Serialize(object data)
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
    }
}