using Etg.Yams.Utils;
using Xunit;

namespace Etg.Yams.Test.Utils
{
    public class AzureUtilsTest
    {
        [Fact]
        public void TestIsEmulator()
        {
            Assert.False(AzureUtils.IsEmulator());
        }
    }
}
