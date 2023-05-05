using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Secrets;

using Moq;

using Notation.Plugin.Protocol;

using Xunit;

namespace Notation.Plugin.AzureKeyVault.Client.Tests
{
    public class KeyVaultClientTests
    {
        [Fact]
        public void TestConstructorWithKeyId()
        {
            string keyId = "https://myvault.vault.azure.net/keys/my-key/123";

            KeyVaultClient keyVaultClient = new KeyVaultClient(keyId);

            Assert.Equal("my-key", keyVaultClient.Name);
            Assert.Equal("123", keyVaultClient.Version);
            Assert.Equal(keyId, keyVaultClient.KeyId);
        }

        [Fact]
        public void TestConstructorWithKeyVaultUrlNameVersion()
        {
            string keyVaultUrl = "https://myvault.vault.azure.net";
            string name = "my-key";
            string version = "123";

            KeyVaultClient keyVaultClient = new KeyVaultClient(keyVaultUrl, name, version);

            Assert.Equal(name, keyVaultClient.Name);
            Assert.Equal(version, keyVaultClient.Version);
            Assert.Equal($"{keyVaultUrl}/keys/{name}/{version}", keyVaultClient.KeyId);
        }

        [Theory]
        [InlineData("https://myvault.vault.azure.net/invalid/my-key/123")]
        [InlineData("https://myvault.vault.azure.net/keys/my-key")]
        [InlineData("https://myvault.vault.azure.net/keys/my-key/")]
        [InlineData("http://myvault.vault.azure.net/keys/my-key/123")]
        public void TestConstructorWithInvalidKeyId(string invalidKeyId)
        {
            Assert.Throws<ValidationException>(() => new KeyVaultClient(invalidKeyId));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void TestConstructorWithEmptyKeyId(string invalidKeyId)
        {
            Assert.Throws<ArgumentNullException>(() => new KeyVaultClient(invalidKeyId));
        }

        private class TestableKeyVaultClient : KeyVaultClient
        {
            public TestableKeyVaultClient(string keyVaultUrl, string name, string version, CryptographyClient cryptoClient)
                : base(keyVaultUrl, name, version)
            {
                this._cryptoClient = new Lazy<CryptographyClient>(() => cryptoClient);
            }

            public TestableKeyVaultClient(string keyVaultUrl, string name, string version, CertificateClient certificateClient)
                : base(keyVaultUrl, name, version)
            {
                this._certificateClient = new Lazy<CertificateClient>(() => certificateClient);
            }

            public TestableKeyVaultClient(string keyVaultUrl, string name, string version, SecretClient secretClient)
                : base(keyVaultUrl, name, version)
            {
                this._secretClient = new Lazy<SecretClient>(() => secretClient);
            }
        }

        private TestableKeyVaultClient CreateMockedKeyVaultClient(SignResult signResult)
        {
            var mockCryptoClient = new Mock<CryptographyClient>(new Uri("https://fake.vault.azure.net/keys/fake-key/123"), new Mock<TokenCredential>().Object);
            mockCryptoClient.Setup(c => c.SignDataAsync(It.IsAny<SignatureAlgorithm>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(signResult);

            return new TestableKeyVaultClient("https://fake.vault.azure.net", "fake-key", "123", mockCryptoClient.Object);
        }

        private TestableKeyVaultClient CreateMockedKeyVaultClient(KeyVaultCertificate certificate)
        {
            var mockCertificateClient = new Mock<CertificateClient>(new Uri("https://fake.vault.azure.net/certificates/fake-certificate/123"), new Mock<TokenCredential>().Object);
            mockCertificateClient.Setup(c => c.GetCertificateVersionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(certificate, new Mock<Response>().Object));

            return new TestableKeyVaultClient("https://fake.vault.azure.net", "fake-certificate", "123", mockCertificateClient.Object);
        }

        private TestableKeyVaultClient CreateMockedKeyVaultClient(KeyVaultSecret secret)
        {
            var mockSecretClient = new Mock<SecretClient>(new Uri("https://fake.vault.azure.net/secrets/fake-secret/123"), new Mock<TokenCredential>().Object);
            mockSecretClient.Setup(c => c.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(secret, new Mock<Response>().Object));
            return new TestableKeyVaultClient("https://fake.vault.azure.net", "fake-certificate", "123", mockSecretClient.Object);
        }

        [Fact]
        public async Task TestSignAsyncReturnsExpectedSignature()
        {
            var signResult = CryptographyModelFactory.SignResult(
                keyId: "https://fake.vault.azure.net/keys/fake-key/123",
                signature: new byte[] { 1, 2, 3 },
                algorithm: SignatureAlgorithm.RS256);

            TestableKeyVaultClient keyVaultClient = CreateMockedKeyVaultClient(signResult);
            byte[] payload = new byte[] { 4, 5, 6 };

            byte[] signature = await keyVaultClient.SignAsync(SignatureAlgorithm.RS256, payload);

            Assert.Equal(signResult.Signature, signature);
        }

        [Fact]
        public async Task TestSignAsyncThrowsExceptionOnInvalidKeyId()
        {
            var signResult = CryptographyModelFactory.SignResult(
                keyId: "https://fake.vault.azure.net/keys/invalid-key/123",
                signature: new byte[] { 1, 2, 3 },
                algorithm: SignatureAlgorithm.RS256);

            TestableKeyVaultClient keyVaultClient = CreateMockedKeyVaultClient(signResult);
            byte[] payload = new byte[] { 4, 5, 6 };

            await Assert.ThrowsAsync<PluginException>(async () => await keyVaultClient.SignAsync(SignatureAlgorithm.RS256, payload));
        }

        [Fact]
        public async Task TestSignAsyncThrowsExceptionOnInvalidAlgorithm()
        {
            var signResult = CryptographyModelFactory.SignResult(
                keyId: "https://fake.vault.azure.net/keys/fake-key/123",
                signature: new byte[] { 1, 2, 3 },
                algorithm: SignatureAlgorithm.RS384);

            TestableKeyVaultClient keyVaultClient = CreateMockedKeyVaultClient(signResult);
            byte[] payload = new byte[] { 4, 5, 6 };
            await Assert.ThrowsAsync<PluginException>(async () => await keyVaultClient.SignAsync(SignatureAlgorithm.RS256, payload));
        }

        [Fact]
        public async Task GetCertificateAsync_ReturnsCertificate()
        {
            var testCertificate = new X509Certificate2(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "rsa_2048.crt"));
            var signResult = CryptographyModelFactory.SignResult(
                keyId: "https://fake.vault.azure.net/keys/fake-key/123",
                signature: new byte[] { 1, 2, 3 },
                algorithm: SignatureAlgorithm.RS384);

            var keyVaultCertificate = CertificateModelFactory.KeyVaultCertificate(
                properties: CertificateModelFactory.CertificateProperties(version: "123"),
                cer: testCertificate.RawData);

            var keyVaultClient = CreateMockedKeyVaultClient(keyVaultCertificate);
            var certificate = await keyVaultClient.GetCertificateAsync();

            Assert.NotNull(certificate);
            Assert.IsType<X509Certificate2>(certificate);
            Assert.Equal("123", keyVaultCertificate.Properties.Version);
            Assert.Equal(testCertificate.RawData, certificate.RawData);
        }

        [Fact]
        public async Task GetCertificateAsyncThrowValidationException()
        {
            var testCertificate = new X509Certificate2(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "rsa_2048.crt"));
            var signResult = CryptographyModelFactory.SignResult(
                keyId: "https://fake.vault.azure.net/keys/fake-key/123",
                signature: new byte[] { 1, 2, 3 },
                algorithm: SignatureAlgorithm.RS384);

            var keyVaultCertificate = CertificateModelFactory.KeyVaultCertificate(
                properties: CertificateModelFactory.CertificateProperties(version: "1234"),
                cer: testCertificate.RawData);

            var keyVaultClient = CreateMockedKeyVaultClient(keyVaultCertificate);

            await Assert.ThrowsAsync<ValidationException>(async () => await keyVaultClient.GetCertificateAsync());
        }

        [Fact]
        public async Task GetCertificateChainAsync_PKCS12()
        {
            var certChainBytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "cert_chain.pfx"));
            var testCertificateChain = new X509Certificate2Collection();
            testCertificateChain.Import(certChainBytes, "", X509KeyStorageFlags.Exportable);
            var properties = SecretModelFactory.SecretProperties();
            properties.ContentType = "application/x-pkcs12";
            var secret = SecretModelFactory.KeyVaultSecret(
                value: Convert.ToBase64String(certChainBytes),
                properties: properties);

            var keyVaultClient = CreateMockedKeyVaultClient(secret);

            var certificateChain = await keyVaultClient.GetCertificateChainAsync();

            Assert.NotNull(certificateChain);
            Assert.IsType<X509Certificate2Collection>(certificateChain);
            Assert.Equal(2, certificateChain.Count);
            Assert.Equal(testCertificateChain[0].RawData, certificateChain[0].RawData);
            Assert.Equal(testCertificateChain[1].RawData, certificateChain[1].RawData);
        }

        [Fact]
        public async Task GetCertificateChainAsync_PEM()
        {
            var certChainBytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "cert_chain.pem"));
            var testCertificateChain = new X509Certificate2Collection();
            testCertificateChain.ImportFromPemFile(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "cert_chain.pem"));
            var properties = SecretModelFactory.SecretProperties();
            properties.ContentType = "application/x-pem-file";
            var secret = SecretModelFactory.KeyVaultSecret(
                value: Encoding.UTF8.GetString(certChainBytes),
                properties: properties);

            var keyVaultClient = CreateMockedKeyVaultClient(secret);

            var certificateChain = await keyVaultClient.GetCertificateChainAsync();

            Assert.NotNull(certificateChain);
            Assert.IsType<X509Certificate2Collection>(certificateChain);
            Assert.Equal(2, certificateChain.Count);
            Assert.Equal(testCertificateChain[0].RawData, certificateChain[0].RawData);
            Assert.Equal(testCertificateChain[1].RawData, certificateChain[1].RawData);
        }

        [Fact]
        public async Task GetCertificateChainAsync_UnknownFormat()
        {
            var properties = SecretModelFactory.SecretProperties();
            properties.ContentType = "application/x-unknown";
            var secret = SecretModelFactory.KeyVaultSecret(
                properties: properties);

            var keyVaultClient = CreateMockedKeyVaultClient(secret);
            await Assert.ThrowsAsync<ValidationException>(async () => await keyVaultClient.GetCertificateChainAsync());
        }
    }
}
