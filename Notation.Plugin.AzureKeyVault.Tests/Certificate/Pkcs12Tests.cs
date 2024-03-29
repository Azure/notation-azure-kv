using System.IO;
using System.Security.Cryptography.Pkcs;
using Notation.Plugin.Protocol;
using Xunit;

namespace Notation.Plugin.AzureKeyVault.Certificate.Tests
{
    public class Pkcs12Tests
    {
        // MAC integrity mode is password(null) and saftContent confidential mode is password(null)
        [Fact]
        public void ReEncode()
        {
            // read the pfx file
            byte[] data = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "cert_chain.pfx"));
            Pkcs12Info originPfx = Pkcs12Info.Decode(data, out _);
            Assert.True(originPfx.IntegrityMode == Pkcs12IntegrityMode.Password);

            // re-encode the pfx file
            byte[] newData = Pkcs12.ReEncode(data);
            Pkcs12Info pfxWithoutMac = Pkcs12Info.Decode(newData, out _);
            Assert.True(pfxWithoutMac.IntegrityMode == Pkcs12IntegrityMode.None);
        }

        [Fact]
        public void ReEncode_WithInvalidMac()
        {
            // read the pfx file
            byte[] data = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "cert_invalid_mac.pfx"));
            Pkcs12Info originPfx = Pkcs12Info.Decode(data, out _);
            Assert.True(originPfx.IntegrityMode == Pkcs12IntegrityMode.Password);

            // re-encode the pfx file
            Assert.Throws<ValidationException>(() => Pkcs12.ReEncode(data));
        }

        [Fact]
        public void ReEncode_withoutMac()
        {
            // read the pfx file
            byte[] data = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "cert_without_mac.pfx"));
            Pkcs12Info originPfx = Pkcs12Info.Decode(data, out _);
            Assert.True(originPfx.IntegrityMode == Pkcs12IntegrityMode.None);

            // re-encode the pfx file
            byte[] newData = Pkcs12.ReEncode(data);
            Pkcs12Info pfxWithoutMac = Pkcs12Info.Decode(newData, out _);
            Assert.True(pfxWithoutMac.IntegrityMode == Pkcs12IntegrityMode.None);
        }

        // MAC integrity mode is password(null) and saftContent confidential mode is none
        [Fact]
        public void ReEncode_akv_imported()
        {
            // read the pfx file
            byte[] data = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "akv_imported_cert.pfx"));
            Pkcs12Info originPfx = Pkcs12Info.Decode(data, out _);
            Assert.True(originPfx.IntegrityMode == Pkcs12IntegrityMode.Password);

            // re-encode the pfx file
            byte[] newData = Pkcs12.ReEncode(data);
            Pkcs12Info pfxWithoutMac = Pkcs12Info.Decode(newData, out _);
            Assert.True(pfxWithoutMac.IntegrityMode == Pkcs12IntegrityMode.None);
        }
    }
}
