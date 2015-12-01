using Etg.Yams.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Etg.Yams.Test.Utils
{
    [TestClass]
    public class AzureUtilsTest
    {
        [TestMethod]
        public void TestIsEmulator()
        {
            Assert.IsFalse(AzureUtils.IsEmulator());
        }
    }
}
