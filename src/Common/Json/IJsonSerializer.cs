using System.Threading.Tasks;

namespace Etg.Yams.Json
{
    public interface IJsonSerializer
    {
        string Serialize(object data);
        Task SerializeAsync(object data);
        T Deserialize<T>(string data);
        Task<T> DeserializeAsync<T>(string data);
    }
}