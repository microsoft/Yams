using Etg.Yams.Client;
using Xunit;

namespace Etg.Yams.Test.Client
{
    public class YamsProcessArgsParserTest
    {
        [Fact]
        public void TestParseOneArg()
        {
            var parser = new ProcessArgsParser();
            var options = parser.ParseArgs(new [] { "--InitializationPipeName", "Foo" });
            Assert.Equal("Foo", options.InitializationPipeName);
            Assert.Null(options.HealthPipeName);
            Assert.Null(options.ExitPipeName);
        }

        [Fact]
        public void TestParseTwoArgs()
        {
            var parser = new ProcessArgsParser();
            var options = parser.ParseArgs(new[] { "--InitializationPipeName", "Foo", "--HealthPipeName", "Bar" });
            Assert.Equal("Foo", options.InitializationPipeName);
            Assert.Equal("Bar", options.HealthPipeName);
            Assert.Null(options.ExitPipeName);
        }

        [Fact]
        public void TestParseAllArgs()
        {
            var parser = new ProcessArgsParser();
            var options = parser.ParseArgs(new[] { "--InitializationPipeName", "Foo", "--HealthPipeName", "Bar", "--ExitPipeName", "abc" });
            Assert.Equal("Foo", options.InitializationPipeName);
            Assert.Equal("Bar", options.HealthPipeName);
            Assert.Equal("abc", options.ExitPipeName);
        }

        [Fact]
        public void TestThatParseArgsIgnoresAdditionalArgs()
        {
            var parser = new ProcessArgsParser();
            var options = parser.ParseArgs(new[] { "blabla", "--InitializationPipeName", "Foo", "--HealthPipeName", "Bar", "--ExitPipeName", "abc", "--Other", "Bar" });
            Assert.Equal("Foo", options.InitializationPipeName);
            Assert.Equal("Bar", options.HealthPipeName);
            Assert.Equal("abc", options.ExitPipeName);
        }

        [Fact]
        public void TestThatParseEmptyArgsIsOk()
        {
            var parser = new ProcessArgsParser();
            var options = parser.ParseArgs(new string[] {});
            Assert.Null(options.InitializationPipeName);
            Assert.Null(options.HealthPipeName);
            Assert.Null(options.ExitPipeName);
        }
    }
}