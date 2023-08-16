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
        public static byte[] RemoveMac(byte[] data)
        {
            Pkcs12Info pfx = Pkcs12Info.Decode(data, out _);
            // only remove the MAC if it is password protected
            if (pfx.IntegrityMode != Pkcs12IntegrityMode.Password)
            {
                return data;
            }
            // verify the MAC with empty password
            if (!pfx.VerifyMac(null))
            {
                throw new ValidationException("Invalid MAC");
            }

            // re-build PFX without MAC
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
