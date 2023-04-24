using Xunit;
using System.Threading.Tasks;
using Notation.Plugin.Protocol;
using Notation.Plugin.AzureKeyVault.Command;

namespace Notation.Plugin.AzureKeyVault.Tests
{
    public class GetPluginMetadataTests
    {
        [Fact]
        public async Task RunAsync_ReturnsExpectedMetadata()
        {
            // Arrange
            var getPluginMetadata = new GetPluginMetadata();

            // Act
            var result = await getPluginMetadata.RunAsync("");
            
            // Assert
            Assert.IsType<GetMetadataResponse>(result);

            var metadata = result as GetMetadataResponse;
            Assert.Equal("azure-kv", metadata?.Name);
            Assert.Equal("Notation Azure Key Vault plugin", metadata?.Description);
            Assert.Equal("1.0.0", metadata?.Version);
            Assert.Equal("https://github.com/Azure/notation-azure-kv", metadata?.Url);
            Assert.Contains("1.0", metadata?.SupportedContractVersions ?? new string[0]);
            Assert.Contains("SIGNATURE_GENERATOR.RAW", metadata?.Capabilities ?? new string[0]);
        }
    }
}
