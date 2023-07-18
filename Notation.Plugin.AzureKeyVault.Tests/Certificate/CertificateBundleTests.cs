using System.IO;
using System.Security.Cryptography.X509Certificates;
using Notation.Plugin.Protocol;
using Xunit;

namespace Notation.Plugin.AzureKeyVault.Certificate.Tests
{
    public class CertificateBundleTests
    {
        [Fact]
        public void Create_WithValidPemFile_ReturnsCertificates()
        {
            // Arrange
            string pemFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "rsa_2048.crt");

            // Act
            X509Certificate2Collection certificates = CertificateBundle.Create(pemFilePath);

            // Assert
            Assert.NotNull(certificates);
            Assert.True(certificates.Count > 0);
        }

        [Fact]
        public void Create_ThrowsPluginException_WhenPemFileIsEmpty()
        {
            // Arrange
            var pemPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "empty.pem");

            // Act & Assert
            Assert.Throws<PluginException>(() => CertificateBundle.Create(pemPath));
        }
    }
}
