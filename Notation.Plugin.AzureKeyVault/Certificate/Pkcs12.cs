using System.Security.Cryptography.Pkcs;
using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault.Certificate
{
    static class Pkcs12
    {
        /// <summary>
        /// Re-encode the PKCS12 data to removed the MAC.
        /// The macOS doesn't support PKCS12 with non-encrypted MAC.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="ValidationException"></exception>
        public static byte[] RemoveMAC(byte[] data)
        {
            Pkcs12Info pfx = Pkcs12Info.Decode(data, out _);
            Pkcs12Builder pfxBuilder = new Pkcs12Builder();
            foreach (var authSafe in pfx.AuthenticatedSafe)
            {
                pfxBuilder.AddSafeContentsUnencrypted(authSafe);
            }
            pfxBuilder.SealWithoutIntegrity();
            return pfxBuilder.Encode();
        }
    }
}
