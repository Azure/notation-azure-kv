using Xunit;
using Moq;
using Azure;
using Notation.Plugin.Protocol;
using System.IO;
using System.Threading.Tasks;
using System;
using Moq.Protected;

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
        // we need this because of method being protected
        internal interface IResponseMock
        {
            bool TryGetHeader(string name, out string value);
        }

        // we need this to be able to define the callback with out parameter
        delegate bool TryGetHeaderCallback(string name, ref string value);


        [Theory]
        [InlineData(200, "{\"error\":{\"message\":\"TestErrorMessage\"}}", "TestErrorMessage")]
        [InlineData(500, "{\"error\":{\"message\":\"TestErrorMessage\"}", "Service request failed.\nStatus: 500\n\nHeaders:\n")]
        [InlineData(500, "{\"error2\":{\"message\":\"TestErrorMessage\"}}", "Service request failed.\nStatus: 500\n\nHeaders:\n")]
        [InlineData(500, "{\"error\":{\"message2\":\"TestErrorMessage\"}}", "Service request failed.\nStatus: 500\n\nHeaders:\n")]
        public void HandleAzureException(int code, string content, string expectedErrorMessage)
        {
            // Arrange
            Mock<Response> responseMock = new Mock<Response>();
            responseMock.SetupGet(r => r.Status).Returns(code);
            responseMock.SetupGet(r => r.Content).Returns(BinaryData.FromString(content));

            // mock headers
            responseMock.CallBase = true;
            responseMock.Protected().As<IResponseMock>().Setup(m => m.TryGetHeader(It.IsAny<string>(), out It.Ref<string>.IsAny))
                   .Returns(new TryGetHeaderCallback((string name, ref string value) =>
                   {
                       value = "ETAG";
                       Console.WriteLine(name);
                       return true;
                   }));

            var exception = new RequestFailedException(responseMock.Object);

            // Act
            var errorResponse = Program.HandleAzureException(exception);

            // Assert exit code 1
            Assert.Equal(expectedErrorMessage, errorResponse.ErrorMessage);
            Assert.Equal("ERROR", errorResponse.ErrorCode);
        }
    }
}
