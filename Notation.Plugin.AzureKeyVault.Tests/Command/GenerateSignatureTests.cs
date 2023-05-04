using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Keys.Cryptography;
using Moq;
using Notation.Plugin.AzureKeyVault.Certificate;
using Notation.Plugin.AzureKeyVault.Client;
using Notation.Plugin.Protocol;
using Xunit;

namespace Notation.Plugin.AzureKeyVault.Command.Tests
{
    public class GenerateSignatureTests
    {
        [Fact]
        public async Task RunAsync_SelfSigned_ReturnsValidGenerateSignatureResponseAsync()
        {
            // Arrange
            var keyId = "https://testvault.vault.azure.net/keys/testkey/123";
            var expectedKeySpec = "RSA-2048";
            var mockCert = new X509Certificate2(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "rsa_2048.crt"));
            var mockSignature = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            var mockKeyVaultClient = new Mock<IKeyVaultClient>();
            // mock GetCertificateAsync
            mockKeyVaultClient.Setup(client => client.GetCertificateAsync())
                              .ReturnsAsync(mockCert);

            // mock SignAsync
            mockKeyVaultClient.Setup(client => client.SignAsync(It.IsAny<SignatureAlgorithm>(), It.IsAny<byte[]>()))
                              .ReturnsAsync(mockSignature);

            var request = new GenerateSignatureRequest(
                contractVersion: "1.0",
                keyId: keyId,
                pluginConfig: null,
                keySpec: expectedKeySpec,
                hashAlgorithm: "SHA-256",
                payload: Encoding.UTF8.GetBytes("Cg=="));

            var generateSignatureCommand = new GenerateSignature(request, mockKeyVaultClient.Object);

            var result = await generateSignatureCommand.RunAsync();

            Assert.IsType<GenerateSignatureResponse>(result);
            var response = result as GenerateSignatureResponse;
            if (response == null)
            {
                throw new System.Exception("response is null");
            }
            Assert.Equal(keyId, response.KeyId);
            Assert.Equal("RSASSA-PSS-SHA-256", response.SigningAlgorithm);
            Assert.Equal(mockSignature, response.Signature);
            Assert.Single(response.CertificateChain);
            Assert.Equal(mockCert.RawData, response.CertificateChain[0]);
        }

        [Fact]
        public async Task RunAsync_ca_certs_ReturnsValidGenerateSignatureResponseAsync()
        {
            // Arrange
            var keyId = "https://testvault.vault.azure.net/keys/testkey/123";
            var expectedKeySpec = "RSA-2048";
            var testRootCert = new X509Certificate2(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "root.crt"));
            var mockCert = new X509Certificate2(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "leaf.crt"));
            var mockSignature = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            var mockKeyVaultClient = new Mock<IKeyVaultClient>();
            // mock GetCertificateAsync
            mockKeyVaultClient.Setup(client => client.GetCertificateAsync())
                              .ReturnsAsync(mockCert);

            // mock SignAsync
            mockKeyVaultClient.Setup(client => client.SignAsync(It.IsAny<SignatureAlgorithm>(), It.IsAny<byte[]>()))
                              .ReturnsAsync(mockSignature);

            var request = new GenerateSignatureRequest(
                contractVersion: "1.0",
                keyId: keyId,
                pluginConfig: new Dictionary<string, string>()
                {
                    ["ca_certs"] = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "root.crt")
                },
                keySpec: expectedKeySpec,
                hashAlgorithm: "SHA-256",
                payload: Encoding.UTF8.GetBytes("Cg=="));

            var generateSignatureCommand = new GenerateSignature(request, mockKeyVaultClient.Object);

            var result = await generateSignatureCommand.RunAsync();

            Assert.IsType<GenerateSignatureResponse>(result);
            var response = result as GenerateSignatureResponse;
            if (response == null)
            {
                throw new System.Exception("response is null");
            }
            Assert.Equal(keyId, response.KeyId);
            Assert.Equal("RSASSA-PSS-SHA-256", response.SigningAlgorithm);
            Assert.Equal(mockSignature, response.Signature);
            Assert.Equal(2, response.CertificateChain.Count);
            Assert.Equal(mockCert.RawData, response.CertificateChain[0]);
            Assert.Equal(testRootCert.RawData, response.CertificateChain[1]);
        }

        [Fact]
        public async Task RunAsync_as_secret_ReturnsValidGenerateSignatureResponseAsync()
        {
            // Arrange
            var keyId = "https://testvault.vault.azure.net/keys/testkey/123";
            var expectedKeySpec = "RSA-2048";
            var mockSignature = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var mockCertChain = CertificateBundle.Create(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "cert_chain.pem"));

            var mockKeyVaultClient = new Mock<IKeyVaultClient>();
            // mock GetCertificateAsync
            mockKeyVaultClient.Setup(client => client.GetCertificateChainAsync())
                              .ReturnsAsync(mockCertChain);

            // mock SignAsync
            mockKeyVaultClient.Setup(client => client.SignAsync(It.IsAny<SignatureAlgorithm>(), It.IsAny<byte[]>()))
                              .ReturnsAsync(mockSignature);

            var request = new GenerateSignatureRequest(
                contractVersion: "1.0",
                keyId: keyId,
                pluginConfig: new Dictionary<string, string>()
                {
                    ["as_secret"] = "true"
                },
                keySpec: expectedKeySpec,
                hashAlgorithm: "SHA-256",
                payload: Encoding.UTF8.GetBytes("Cg=="));

            var generateSignatureCommand = new GenerateSignature(request, mockKeyVaultClient.Object);

            var result = await generateSignatureCommand.RunAsync();

            Assert.IsType<GenerateSignatureResponse>(result);
            var response = result as GenerateSignatureResponse;
            if (response == null)
            {
                throw new System.Exception("response is null");
            }
            Assert.Equal(keyId, response.KeyId);
            Assert.Equal("RSASSA-PSS-SHA-256", response.SigningAlgorithm);
            Assert.Equal(mockSignature, response.Signature);
            Assert.Equal(2, response.CertificateChain.Count);
            Assert.Equal(mockCertChain[0].RawData, response.CertificateChain[0]);
            Assert.Equal(mockCertChain[1].RawData, response.CertificateChain[1]);
        }

        [Fact]
        public void Constructor_Valid()
        {
            string validInputJson = "{\"contractVersion\":\"1.0\",\"keyId\":\"https://notationakvtest.vault.azure.net/keys/dotnetPluginCert/b6046b30d069458886de94b0ac9ed121\",\"keySpec\":\"RSA-2048\",\"hashAlgorithm\":\"SHA-256\",\"payload\":\"Cg==\"}";

            Assert.Null(Record.Exception(() => new GenerateSignature(validInputJson)));
        }

        [Fact]
        public void Constructor_Invalid()
        {
            string InvalidInputJson = "null";

            Assert.Throws<ValidationException>(() => new GenerateSignature(InvalidInputJson));
        }
    }
}