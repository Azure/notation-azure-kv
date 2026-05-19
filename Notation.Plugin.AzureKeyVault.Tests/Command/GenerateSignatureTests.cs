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
            var mockSignature = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var mockKeyVaultClient = new Mock<IKeyVaultClient>();

            // mock GetCertificateAsync
            var mockCert = new X509Certificate2(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "leaf.crt"));
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
                    ["self_signed"] = "true"
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
        public async Task RunAsync_default_ReturnsValidGenerateSignatureResponseAsync()
        {
            // Arrange
            var keyId = "https://testvault.vault.azure.net/keys/testkey/123";
            var expectedKeySpec = "RSA-2048";
            var mockSignature = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var mockKeyVaultClient = new Mock<IKeyVaultClient>();

            // mock GetCertificateChainAsync
            var mockCertChain = CertificateBundle.Create(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "cert_chain.pem"));
            mockKeyVaultClient.Setup(client => client.GetCertificateChainAsync())
                              .ReturnsAsync(mockCertChain);

            // mock SignAsync
            mockKeyVaultClient.Setup(client => client.SignAsync(It.IsAny<SignatureAlgorithm>(), It.IsAny<byte[]>()))
                              .ReturnsAsync(mockSignature);

            var request = new GenerateSignatureRequest(
                contractVersion: "1.0",
                keyId: keyId,
                pluginConfig: new Dictionary<string, string>() { },
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

        [Fact]
        public async Task RunAsync_NoSecertsGetPermission()
        {
            // Arrange
            var keyId = "https://testvault.vault.azure.net/keys/testkey/123";
            var expectedKeySpec = "RSA-2048";
            var mockSignature = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var mockKeyVaultClient = new Mock<IKeyVaultClient>();

            // mock GetCertificateChainAsync
            var mockCertChain = CertificateBundle.Create(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "cert_chain.pem"));
            mockKeyVaultClient.Setup(client => client.GetCertificateChainAsync())
                              .ThrowsAsync(new Azure.RequestFailedException("does not have secrets get permission"));

            var request = new GenerateSignatureRequest(
                contractVersion: "1.0",
                keyId: keyId,
                pluginConfig: new Dictionary<string, string>() { },
                keySpec: expectedKeySpec,
                hashAlgorithm: "SHA-256",
                payload: Encoding.UTF8.GetBytes("Cg=="));

            var generateSignatureCommand = new GenerateSignature(request, mockKeyVaultClient.Object);

            await Assert.ThrowsAsync<PluginException>(async () => await generateSignatureCommand.RunAsync());
        }

        [Fact]
        public async Task RunAsync_OtherRequestFailedException()
        {
            // Arrange
            var keyId = "https://testvault.vault.azure.net/keys/testkey/123";
            var expectedKeySpec = "RSA-2048";
            var mockSignature = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var mockKeyVaultClient = new Mock<IKeyVaultClient>();

            // mock GetCertificateChainAsync
            var mockCertChain = CertificateBundle.Create(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "cert_chain.pem"));
            mockKeyVaultClient.Setup(client => client.GetCertificateChainAsync())
                              .ThrowsAsync(new Azure.RequestFailedException("RequestFailedException"));

            var request = new GenerateSignatureRequest(
                contractVersion: "1.0",
                keyId: keyId,
                pluginConfig: new Dictionary<string, string>() { },
                keySpec: expectedKeySpec,
                hashAlgorithm: "SHA-256",
                payload: Encoding.UTF8.GetBytes("Cg=="));

            var generateSignatureCommand = new GenerateSignature(request, mockKeyVaultClient.Object);

            await Assert.ThrowsAsync<Azure.RequestFailedException>(async () => await generateSignatureCommand.RunAsync());
        }

        [Fact]
        public async Task RunAsync_SelfSignedWithCaCerts()
        {
            // Arrange
            var keyId = "https://testvault.vault.azure.net/keys/testkey/123";
            var expectedKeySpec = "RSA-2048";
            var mockSignature = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var mockKeyVaultClient = new Mock<IKeyVaultClient>();

            // mock GetCertificateChainAsync
            var mockCertChain = CertificateBundle.Create(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "cert_chain.pem"));
            var request = new GenerateSignatureRequest(
                contractVersion: "1.0",
                keyId: keyId,
                pluginConfig: new Dictionary<string, string>() {
                    { "self_signed", "true" },
                    { "ca_certs", "/test/cert.pem" }
                },
                keySpec: expectedKeySpec,
                hashAlgorithm: "SHA-256",
                payload: Encoding.UTF8.GetBytes("Cg=="));

            var generateSignatureCommand = new GenerateSignature(request, mockKeyVaultClient.Object);

            await Assert.ThrowsAsync<PluginException>(async () => await generateSignatureCommand.RunAsync());
        }

        [Theory]
        [InlineData("invalid-scheme")]
        public async Task RunAsync_InvalidSigningScheme_ThrowsValidationException(string scheme)
        {
            // Arrange
            var keyId = "https://testvault.vault.azure.net/keys/testkey/123";
            var rsaCert = new X509Certificate2(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "rsa_2048.crt"));
            var mockKeyVaultClient = new Mock<IKeyVaultClient>();
            mockKeyVaultClient.Setup(client => client.GetCertificateAsync())
                              .ReturnsAsync(rsaCert);

            var request = new GenerateSignatureRequest(
                contractVersion: "1.0",
                keyId: keyId,
                pluginConfig: new Dictionary<string, string>()
                {
                    ["self_signed"] = "true",
                    [GenerateSignature.SigningSchemeConfigKey] = scheme
                },
                keySpec: "RSA-2048",
                hashAlgorithm: "SHA-256",
                payload: Encoding.UTF8.GetBytes("Cg=="));

            var generateSignatureCommand = new GenerateSignature(request, mockKeyVaultClient.Object);

            // Act & Assert: invalid scheme must surface as ValidationException
            // (and not as ArgumentException from the helpers), and must reject
            // before any AKV SignAsync call.
            var ex = await Assert.ThrowsAsync<ValidationException>(
                async () => await generateSignatureCommand.RunAsync());
            Assert.Contains(scheme, ex.Message);
            Assert.Contains(SigningSchemes.RSASSA_PSS, ex.Message);
            Assert.Contains(SigningSchemes.RSASSA_PKCS1_V1_5, ex.Message);
            mockKeyVaultClient.Verify(
                c => c.SignAsync(It.IsAny<SignatureAlgorithm>(), It.IsAny<byte[]>()),
                Times.Never);
        }

        [Theory]
        [InlineData("ec_256.crt", "rsassa-pkcs1-v1_5")]
        [InlineData("ec_384.crt", "rsassa-pkcs1-v1_5")]
        [InlineData("ec_521.crt", "rsassa-pkcs1-v1_5")]
        [InlineData("ec_256.crt", "RSASSA-PKCS1-V1_5")]
        public async Task RunAsync_PKCS1SchemeWithECKey_ThrowsValidationException(string ecCertFile, string scheme)
        {
            // Arrange
            var keyId = "https://testvault.vault.azure.net/keys/testkey/123";
            var ecCert = new X509Certificate2(Path.Combine(Directory.GetCurrentDirectory(), "TestData", ecCertFile));
            var mockKeyVaultClient = new Mock<IKeyVaultClient>();
            mockKeyVaultClient.Setup(client => client.GetCertificateAsync())
                              .ReturnsAsync(ecCert);

            var request = new GenerateSignatureRequest(
                contractVersion: "1.0",
                keyId: keyId,
                pluginConfig: new Dictionary<string, string>()
                {
                    ["self_signed"] = "true",
                    [GenerateSignature.SigningSchemeConfigKey] = scheme
                },
                keySpec: "EC-256",
                hashAlgorithm: "SHA-256",
                payload: Encoding.UTF8.GetBytes("Cg=="));

            var generateSignatureCommand = new GenerateSignature(request, mockKeyVaultClient.Object);

            // Act & Assert: must reject before any AKV SignAsync call.
            var ex = await Assert.ThrowsAsync<ValidationException>(
                async () => await generateSignatureCommand.RunAsync());
            Assert.Contains(SigningSchemes.RSASSA_PKCS1_V1_5, ex.Message);
            mockKeyVaultClient.Verify(
                c => c.SignAsync(It.IsAny<SignatureAlgorithm>(), It.IsAny<byte[]>()),
                Times.Never);
        }

        [Theory]
        [InlineData("rsa_2048.crt", "RS256", "RSASSA-PKCS1-v1_5-SHA-256")]
        [InlineData("rsa_3072.crt", "RS384", "RSASSA-PKCS1-v1_5-SHA-384")]
        [InlineData("rsa_4096.crt", "RS512", "RSASSA-PKCS1-v1_5-SHA-512")]
        public async Task RunAsync_PKCS1SchemeWithRSAKey_ReturnsPKCS1Response(
            string rsaCertFile,
            string expectedAkvAlgorithm,
            string expectedSigningAlgorithm)
        {
            // Arrange
            var keyId = "https://testvault.vault.azure.net/keys/testkey/123";
            var mockSignature = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var rsaCert = new X509Certificate2(Path.Combine(Directory.GetCurrentDirectory(), "TestData", rsaCertFile));
            var mockKeyVaultClient = new Mock<IKeyVaultClient>();
            mockKeyVaultClient.Setup(client => client.GetCertificateAsync())
                              .ReturnsAsync(rsaCert);

            SignatureAlgorithm? capturedAlgorithm = null;
            mockKeyVaultClient.Setup(client => client.SignAsync(It.IsAny<SignatureAlgorithm>(), It.IsAny<byte[]>()))
                              .Callback<SignatureAlgorithm, byte[]>((algo, _) => capturedAlgorithm = algo)
                              .ReturnsAsync(mockSignature);

            var request = new GenerateSignatureRequest(
                contractVersion: "1.0",
                keyId: keyId,
                pluginConfig: new Dictionary<string, string>()
                {
                    ["self_signed"] = "true",
                    [GenerateSignature.SigningSchemeConfigKey] = SigningSchemes.RSASSA_PKCS1_V1_5
                },
                keySpec: "RSA-2048",
                hashAlgorithm: "SHA-256",
                payload: Encoding.UTF8.GetBytes("Cg=="));

            var generateSignatureCommand = new GenerateSignature(request, mockKeyVaultClient.Object);

            // Act
            var result = await generateSignatureCommand.RunAsync();

            // Assert: end-to-end wiring routes RSASSA-PKCS1-v1_5 through the
            // PKCS1 helpers (AKV call uses RS*, response uses RSASSA-PKCS1-v1_5-SHA-*).
            var response = Assert.IsType<GenerateSignatureResponse>(result);
            Assert.Equal(keyId, response.KeyId);
            Assert.Equal(expectedSigningAlgorithm, response.SigningAlgorithm);
            Assert.Equal(mockSignature, response.Signature);
            Assert.NotNull(capturedAlgorithm);
            Assert.Equal(new SignatureAlgorithm(expectedAkvAlgorithm), capturedAlgorithm.Value);
        }
    }
}
