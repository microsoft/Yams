using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Etg.Yams.Tools.Test.Utils
{
    public static class AssertUtils
    {
        public static void ContainsSameElementsInAnyOrder<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            Assert.IsTrue(new HashSet<T>(expected).SetEquals(actual),
                "The two enumerables do not contain the same set of elements");
        }
    }
}