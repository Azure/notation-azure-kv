using System.IO;
using System.Security.Cryptography.Pkcs;
using Xunit;

namespace Notation.Plugin.AzureKeyVault.Certificate.Tests
{
    public class Pkcs12Tests
    {
        [Fact]
        public void SealWithoutIntegrity()
        {
            // read the pfx file
            byte[] data = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "cert_chain.pfx"));
            Pkcs12Info originPfx = Pkcs12Info.Decode(data, out _);
            Assert.True(originPfx.IntegrityMode == Pkcs12IntegrityMode.Password);

            // decrypt the pfx file
            byte[] decryptedData = Pkcs12.SealWithoutIntegrity(data);
            Pkcs12Info decryptedPfx = Pkcs12Info.Decode(decryptedData, out _);
            Assert.True(decryptedPfx.IntegrityMode == Pkcs12IntegrityMode.None);
        }
    }
}
