using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Moq;
using Notation.Plugin.AzureKeyVault.Client;
using Notation.Plugin.Protocol;
using Xunit;

namespace Notation.Plugin.AzureKeyVault.Command.Tests
{
    public class DescribeKeyTests
    {
        [Fact]
        public async Task RunAsync_ReturnsValidDescribeKeyResponseAsync()
        {
            // Arrange
            var keyId = "https://testvault.vault.azure.net/keys/testkey/123";
            var expectedKeySpec = "RSA-2048";
            var mockCert = new X509Certificate2(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "rsa_2048.crt"));

            var mockKeyVaultClient = new Mock<IKeyVaultClient>();
            mockKeyVaultClient.Setup(client => client.GetCertificateAsync()).ReturnsAsync(mockCert);

            var request = new DescribeKeyRequest(contractVersion: "1.0", keyId);
            var describeKeyCommand = new DescribeKey(request, mockKeyVaultClient.Object);

            // Act
            var result = await describeKeyCommand.RunAsync();

            // Assert
            Assert.IsType<DescribeKeyResponse>(result);
            var response = result as DescribeKeyResponse;
            if (response == null)
            {
                throw new System.Exception("response is null");
            }
            Assert.Equal(keyId, response.KeyId);
            Assert.Equal(expectedKeySpec, response.KeySpec);
        }

        [Fact]
        public void Constructor_ThrowsValidationException_WhenInvalidInput()
        {
            // Arrange
            string invalidInputJson = "null";

            // Act & Assert
            Assert.Throws<ValidationException>(() => new DescribeKey(invalidInputJson));
        }

        [Fact]
        public void Constructor_Valid()
        {
            // Arrange
            string validInputJson = "{\"contractVersion\":\"1.0\",\"keyId\":\"https://notationakvtest.vault.azure.net/keys/dotnetPluginCertPKCS12/3f06c6eeac0640ea9f93cd0bf69d2f17\"}";

            // Act & Assert
            Assert.Null(Record.Exception(() => new DescribeKey(validInputJson)));
        }
    }
}
