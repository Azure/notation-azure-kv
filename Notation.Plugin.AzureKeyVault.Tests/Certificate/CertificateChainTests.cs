using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Notation.Plugin.Protocol;
using Xunit;

namespace Notation.Plugin.AzureKeyVault.Certificate.Tests
{
    public class CertificateChainTests
    {
        [Fact]
        public void Build_WithValidLeafAndCertificateBundle_BuildsCertificateChain()
        {
            // Arrange
            X509Certificate2 leafCert = new X509Certificate2(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "leaf.crt"));
            X509Certificate2Collection certificateBundle = CertificateBundle.Create(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "root.crt"));

            // Act
            List<byte[]> certificateChain = CertificateChain.Build(leafCert, certificateBundle);

            // Assert
            Assert.NotNull(certificateChain);
            Assert.True(certificateChain.Count > 0);
        }
    }
}
