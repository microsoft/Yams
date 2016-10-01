using System.Collections;

namespace Etg.Yams.Utils
{
    public static class HashCodeUtils
    {
        public static int GetHashCode(IEnumerable enumerable)
        {
            int hash = 19;
            foreach (var element in enumerable)
            {
                hash = hash * 31 + element.GetHashCode();
            }
            return hash;
        }
    }
}