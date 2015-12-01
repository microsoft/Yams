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
                return await ParseData<T>(await r.ReadToEndAsync());
            }
        }

        public static Task<T> ParseData<T>(string data)
        {
            return Task.Run(() => JsonConvert.DeserializeObject<T>(data));
        }
    }
}
