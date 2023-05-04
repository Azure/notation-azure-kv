using System.IO;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Notation.Plugin.AzureKeyVault.Certificate.Tests
{
    public class CertificateBundleTests
    {
        [Fact]
        public void Create_WithValidPemFile_ReturnsCertificates()
        {
            // Arrange
            string pemFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "rsa_2048_cert.pem");

            // Act
            X509Certificate2Collection certificates = CertificateBundle.Create(pemFilePath);

            // Assert
            Assert.NotNull(certificates);
            Assert.True(certificates.Count > 0);
        }
    }
}
