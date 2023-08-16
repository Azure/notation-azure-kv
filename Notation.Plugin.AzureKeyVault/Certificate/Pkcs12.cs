using System.Security.Cryptography.Pkcs;
using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault.Certificate
{
    static class Pkcs12
    {
        // Decrypt the PKCS12 data to removed the MAC integrity check.
        // macOS doesn't support PKCS12 with MAC that doesn't have a password.
        public static byte[] Decrypt(byte[] data)
        {
            int consumed;
            Pkcs12Info pfx = Pkcs12Info.Decode(data, out consumed);
            if (consumed == 0)
            {
                throw new ValidationException("Invalid PKCS12 data");
            }
            Pkcs12Builder pfxBuilder = new Pkcs12Builder();
            foreach (var authSafe in pfx.AuthenticatedSafe)
            {
                switch (authSafe.ConfidentialityMode)
                {
                    case Pkcs12ConfidentialityMode.Password:
                        authSafe.Decrypt(new byte[0]);
                        break;
                    case Pkcs12ConfidentialityMode.PublicKey:
                        throw new ValidationException($"Unsupported PKCS12 confidentiality mode: {authSafe.ConfidentialityMode}");
                }
                pfxBuilder.AddSafeContentsUnencrypted(authSafe);
            }
            pfxBuilder.SealWithoutIntegrity();
            return pfxBuilder.Encode();
        }
    }
}
