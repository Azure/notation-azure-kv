using Xunit;
using Moq;
using Notation.Plugin.AzureKeyVault.Command;
using Notation.Plugin.Protocol;
using System.IO;
using System.Threading.Tasks;
using System;

namespace Notation.Plugin.AzureKeyVault.Tests
{
    public class ProgramTests
    {
        [Fact]
        public async Task Main_ExecutesWithoutException()
        {
            var args = new[] { "get-plugin-metadata" };

            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                await Program.Main(args);

                Assert.NotEmpty(sw.ToString());
            }
        }

        [Fact]
        public async Task ExecuteAsync_ThrowsOnInvalidCommand()
        {
            var args = new[] { "invalid-command" };

            await Assert.ThrowsAsync<ValidationException>(() => Program.ExecuteAsync(args));
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsHelpOnNoArgs()
        {
            var args = new string[0];

            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                await Program.ExecuteAsync(args);

                var output = sw.ToString();

                Assert.Contains("Usage:", output);
                Assert.Contains("Commands:", output);
                Assert.Contains("Documentation:", output);
            }
        }

        [Fact]
        public async Task ExecuteAsync_HandlesValidCommands()
        {
            var args = new[] { "get-plugin-metadata" };

            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                await Program.ExecuteAsync(args);

                var output = sw.ToString();

                Assert.NotEmpty(output);
            }
        }

        [Theory]
        [InlineData("describe-key")]
        [InlineData("generate-signature")]
        public async Task ExecuteAsync_HandlesInvalidCommands(string command)
        {
            var args = new[] { command };

            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);
                Console.SetIn(new StringReader(""));

                await Assert.ThrowsAsync<ValidationException>(() => Program.ExecuteAsync(args));
            }
        }
    }
}
