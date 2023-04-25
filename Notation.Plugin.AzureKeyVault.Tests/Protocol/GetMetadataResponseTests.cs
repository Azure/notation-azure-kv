using Xunit;

namespace Notation.Plugin.Protocol.Tests
{
    public class GetMetadataResponseTests
    {
        [Fact]
        public void GetMetadataResponse_CreatesInstance_WithCorrectValues()
        {
            // Arrange
            string name = "Test Plugin";
            string description = "A test plugin for Notation";
            string version = "1.0.0";
            string url = "https://github.com/example/test-plugin";
            string[] supportedContractVersions = new[] { "1.0" };
            string[] capabilities = new[] { "describe-key", "generate-signature" };

            // Act
            GetMetadataResponse response = new GetMetadataResponse(name, description, version, url, supportedContractVersions, capabilities);

            // Assert
            Assert.Equal(name, response.Name);
            Assert.Equal(description, response.Description);
            Assert.Equal(version, response.Version);
            Assert.Equal(url, response.Url);
            Assert.Equal(supportedContractVersions, response.SupportedContractVersions);
            Assert.Equal(capabilities, response.Capabilities);
        }
    }
}
