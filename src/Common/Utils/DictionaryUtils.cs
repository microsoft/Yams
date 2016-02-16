using System.Collections.Generic;
using System.Linq;

namespace Etg.Yams.Utils
{
    public static class DictionaryUtils
    {
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> roDictionary)
        {
            return roDictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}