using System.IO;
using System.Security.Cryptography.X509Certificates;
using Notation.Plugin.Protocol;
using Xunit;

namespace Notation.Plugin.AzureKeyVault.Certificate.Tests
{
    public class CertificateChainTests
    {
        [Fact]
        public void Build_Empty()
        {
            // Arrange
            X509Certificate2Collection certs = new X509Certificate2Collection();

            // Act
            Assert.Throws<PluginException>(() => CertificateChain.Build(certs));
        }

        [Theory]
        [InlineData("leaf.pem", "leaf.pem", false)] // 1: non-self-signed leaf cert
        [InlineData("self_signed_leaf.pem", "self_signed_leaf.pem", true)] // 1: self-signed leaf cert
        [InlineData("2self_signed_leaf.pem", "2self_signed_leaf.pem", false)] // 2: duplicated self-signed leaf cert
        [InlineData("leaf_unknown.pem", "leaf_unknown.pem", false)] // 2: leaf + unknown cert
        [InlineData("leaf_root.pem", "leaf_root.pem", true)] // 2: leaf + root certs
        [InlineData("root_leaf.pem", "leaf_root.pem", true)] // 2: unordered root + leaf certs
        [InlineData("leaf_root_without_inter.pem", "leaf_root_without_inter.pem", false)] // 3: leaf + root certs without intermediate cert
        [InlineData("2root_leaf.pem", "2root_leaf.pem", false)] // 3: duplicated root + leaf certs
        [InlineData("root_leaf1_leaf2.pem", "root_leaf1_leaf2.pem", false)] // 2: root + leaf1 + leaf2 certs
        [InlineData("leaf_root_unknown.pem", "leaf_root_unknown.pem", false)] // 3: leaf + root + unknown certs
        [InlineData("leaf_inter.pem", "leaf_inter.pem", false)] // 2: leaf + intermediate certs
        [InlineData("leaf_inter_root.pem", "leaf_inter_root.pem", true)] // 3: leaf + inter + root certs
        [InlineData("inter_root_leaf.pem", "leaf_inter_root.pem", true)] // 3: unordered inter + root + leaf certs
        [InlineData("leaf_inter_root_otherinter.pem", "leaf_inter_root_otherinter.pem", false)] // 3: leaf + inter + root + other intermediate cert certs
        [InlineData("leaf_inter_root_unknown.pem", "leaf_inter_root_unknown.pem", false)] // 4: leaf + inter + root + unknown certs
        [InlineData("leaf_inter2_inter1_unknown.pem", "leaf_inter2_inter1_unknown.pem", false)] // 4: leaf + inter2 + inter1 + unknown certs
        [InlineData("leaf_inter2_inter1_root.pem", "leaf_inter2_inter1_root.pem", true)] // 4: leaf + inter2 + inter1 + root certs
        [InlineData("inter2_inter1_root_leaf.pem", "leaf_inter2_inter1_root.pem", true)] // 4: inter2 + inter1 + root + leaf certs
        public void Build(string certName, string targetChainName, bool isValid)
        {
            // Arrange
            X509Certificate2Collection certBundle = CertificateBundle.Create(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "chain", certName));
            X509Certificate2Collection targetChain = CertificateBundle.Create(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "chain", targetChainName));

            // Act
            if (isValid)
            {
                var certificateChain = CertificateChain.Build(certBundle);

                // Assert
                for (int i = 0; i < certificateChain.Count; i++)
                {
                    Assert.Equal(targetChain[i].Thumbprint, certificateChain[i].Thumbprint);
                }
            }
            else
            {
                Assert.Throws<PluginException>(() => CertificateChain.Build(certBundle));
            }
        }
    }
}
