using System.Collections.Generic;
using Xunit;

namespace Etg.Yams.TestUtils
{
    public static class AssertUtils
    {
        public static void ContainsSameElementsInAnyOrder<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            Assert.True(new HashSet<T>(expected).SetEquals(actual),
                "The two enumerables do not contain the same set of elements");
        }
    }
}