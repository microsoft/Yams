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
            var options = parser.ParseArgs(new[] { "blabla", "--InitializationPipeName", "Foo", "--HealthPipeName", "Bar", "--ExitPipeName", "abc", "--Other", "Bar", "--AppName", "App", "--AppVersion", "1.2.3-alpha4" });
            Assert.Equal("App", options.AppName);
            Assert.Equal("1.2.3-alpha4", options.AppVersion);
            Assert.Equal("Foo", options.InitializationPipeName);
            Assert.Equal("Bar", options.HealthPipeName);
            Assert.Equal("abc", options.ExitPipeName);
        }

        [Fact]
        public void TestParseAppNameArg()
        {
            var parser = new ProcessArgsParser();
            var options = parser.ParseArgs(new[] { "--AppName", "Foo" });
            Assert.Equal("Foo", options.AppName);
            Assert.Null(options.AppVersion);
        }

        [Fact]
        public void TestParseAppVersionArg()
        {
            var parser = new ProcessArgsParser();
            var options = parser.ParseArgs(new[] { "--AppVersion", "1.2.3-alpha4" });
            Assert.Equal("1.2.3-alpha4", options.AppVersion);
            Assert.Null(options.AppName);
        }

        [Fact]
        public void TestThatParseEmptyArgsIsOk()
        {
            var parser = new ProcessArgsParser();
            var options = parser.ParseArgs(new string[] {});
            Assert.Null(options.AppName);
            Assert.Null(options.AppVersion);
            Assert.Null(options.InitializationPipeName);
            Assert.Null(options.HealthPipeName);
            Assert.Null(options.ExitPipeName);
        }
    }
}