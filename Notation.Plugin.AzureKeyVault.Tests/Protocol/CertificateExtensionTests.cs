using System.IO;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Notation.Plugin.Protocol.Tests
{
    public class CertificateExtensionTests
    {
        [Theory]
        [InlineData("RSA", 2048)]
        [InlineData("RSA", 3072)]
        [InlineData("RSA", 4096)]
        [InlineData("EC", 256)]
        [InlineData("EC", 384)]
        [InlineData("EC", 521)]
        public void KeySpec_ValidKeySize_ReturnsKeySpec(string keyType, int keySize)
        {
            // Arrange
            X509Certificate2 certificate = LoadCertificate(keyType, keySize);

            // Act
            KeySpec result = certificate.KeySpec();

            // Assert
            Assert.Equal(keyType, result.Type.ToString());
            Assert.Equal(keySize, result.Size);
        }

        [Theory]
        [InlineData("RSA", 1024)]
        [InlineData("EC", 163)]
        public void KeySpec_InvalidKeySize_ThrowsValidationException(string keyType, int keySize)
        {
            // Arrange
            X509Certificate2 certificate = LoadCertificate(keyType, keySize);

            // Act & Assert
            Assert.Throws<ValidationException>(() => certificate.KeySpec());
        }

        [Fact]
        public void KeySpec_UnsupportedPublicKeyType_ThrowsValidationException()
        {
            // Arrange
            X509Certificate2 certificate = LoadCertificate("dsa", 2048);

            // Act & Assert
            Assert.Throws<ValidationException>(() => certificate.KeySpec());
        }

        // <summary>
        // Load certificate from file.
        // </summary>
        private static X509Certificate2 LoadCertificate(string keyType, int keySize)
        {
            var certName = $"{keyType.ToLower()}_{keySize}_cert.pem";
            return new X509Certificate2(Path.Combine(Directory.GetCurrentDirectory(), "TestData", certName));
        }

    }
}
