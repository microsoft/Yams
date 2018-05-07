using Etg.Yams.Utils;
using System;
using System.Linq;
using System.Management.Automation;
using System.Security;
using Xunit;

namespace Etg.Yams.Test.Process
{
    public class EnvironmentFixture : IDisposable
    {
        readonly string originalMachinePath;
        public EnvironmentFixture()
        {
            originalMachinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
        }

        public void Dispose()
        {
            try
            {
                Environment.SetEnvironmentVariable("PATH", originalMachinePath, EnvironmentVariableTarget.Machine);
            }
            catch (SecurityException e)
            {
                throw new Exception("Testing environment needs Admin permission to test changing the machine path -- it will be restored after the test!", e);
            }
        }
    }

    public class EnvironmentTest: IClassFixture<EnvironmentFixture>
    {
        [Theory]
        [InlineData("C:\\Windows;C:\\Foo;", "C:\\Windows;D:\\Bar\\", "C:\\Windows;C:\\Foo;D:\\Bar\\;")]
        [InlineData("C:\\Windows;C:\\Foo;", null, "C:\\Windows;C:\\Foo;")]
        [InlineData(null, "C:\\Windows;C:\\Foo\\;", "C:\\Windows;C:\\Foo\\;")]
        [InlineData("C:\\Windows;C:\\Foo", "C:\\Windows;C:\\Foo\\;", "C:\\Windows;C:\\Foo;C:\\Foo\\;")]
        public void TestMergePath(string processPath, string machinePath, string expectedResult)
        {
            string actualResult = EnvironmentUtils.MergePath(processPath, machinePath);
            Assert.Equal(actualResult, expectedResult);
        }

        [Fact]
        public void TestEnvironmentExpansion()
        {
            // Setup: Add an entry to the machine path from another process
            using (PowerShell shell = PowerShell.Create())
            {
                shell.AddScript("[Environment]::SetEnvironmentVariable(\"Path\", $env:Path + \";%ProgramFiles%\\Foo\", [EnvironmentVariableTarget]::Machine)");
                shell.Invoke();

                Assert.False(shell.HadErrors, "Could not modify the machine path using powershell - please run as administrator to execute this test");
            }

            // Get expanded paths
            var processPath = EnvironmentUtils.GetPath(EnvironmentVariableTarget.Process);
            var modifiedMachinePath = EnvironmentUtils.GetPath(EnvironmentVariableTarget.Machine);

            // Check expansion
            string expandedProgramFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Foo";
            Assert.True(modifiedMachinePath.Contains(expandedProgramFolder));

            // Check splitting
            var splitProcessPath = EnvironmentUtils.SplitPath(processPath);
            Assert.False(splitProcessPath.Contains(modifiedMachinePath));

            var splitMachinePath = EnvironmentUtils.SplitPath(modifiedMachinePath);
            Assert.True(splitMachinePath.Contains(expandedProgramFolder));

            // Check modification pickup
            var missingPath = splitMachinePath.Except(splitProcessPath);
            Assert.True(missingPath.Count() == 1);

            // Check result
            string mergedPath = EnvironmentUtils.MergePath(processPath, modifiedMachinePath);
            Assert.True(mergedPath.Contains(processPath.First()));
            Assert.True(mergedPath.Contains(expandedProgramFolder));
        }
    }
}
