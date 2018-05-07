using Etg.Yams.Os;
using System;
using Xunit;

namespace Etg.Yams.Test.System
{
    public class SystemExtensionsTest
    {
        [Theory]
        [InlineData("C:\\Windows;C:\\Foo;", "C:\\Windows;D:\\Bar\\", "C:\\Windows;C:\\Foo;D:\\Bar\\;")]
        [InlineData("C:\\Windows;C:\\Foo;", null, "C:\\Windows;C:\\Foo;")]
        [InlineData(null, "C:\\Windows;C:\\Foo\\;", "C:\\Windows;C:\\Foo\\;")]
        [InlineData("C:\\Windows;C:\\Foo", "C:\\Windows;C:\\Foo\\;", "C:\\Windows;C:\\Foo;C:\\Foo\\;")]
        public void TestGetPath(string processPath, string machinePath, string expectedResult)
        {
            ISystem systemStub = new StubISystem()
                .GetEnvironmentVariable((string name, EnvironmentVariableTarget target) =>
                {
                    if (name != "PATH")
                    {
                        return null;
                    }
                    return target == EnvironmentVariableTarget.Machine ? machinePath : processPath;
                });
            string actualResult = systemStub.GetPathEnvironmentVariable(); // calling extension method
            Assert.Equal(actualResult, expectedResult);
        }
    }
}
