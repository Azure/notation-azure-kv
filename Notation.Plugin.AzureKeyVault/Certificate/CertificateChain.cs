using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault.Certificate
{
    /// <summary>
    /// Helper class to build certificate chain.
    /// </summary>
    static class CertificateChain
    {
        /// <summary>
        /// Build a certificate chain from a leaf certificate and a 
        /// certificate bundle.
        /// 
        /// <param name="certificateBundle">The certificate bundle.</param>
        /// <param name="leafCert">The leaf certificate.</param>
        /// <returns>A list of raw certificates in a chain.</returns>
        /// </summary>
        public static List<byte[]> Build(X509Certificate2 leafCert, X509Certificate2Collection certificateBundle)
        {
            X509Chain chain = new X509Chain();
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.AddRange(certificateBundle);

            try
            {
                bool isValid = chain.Build(leafCert);
                if (!isValid)
                {
                    throw new ValidationException("Certificate is invalid");
                }
            }
            catch (CryptographicException e)
            {
                throw new ValidationException($"Failed to build the X509 chain. {e.Message} The certificate bundle is unreadable. Please ensure the certificate bundle matches the specific certifcate.");
            }

            foreach (X509ChainStatus status in chain.ChainStatus)
            {
                if (status.Status == X509ChainStatusFlags.PartialChain)
                {
                    throw new ValidationException("Failed to build the X509 chain up to the root certificate. The provided certificate bundle either does not match or does not contain enough certificates to build a complete chain. To resolve this issue, provide the intermediate and root certificates by passing the certificate bundle file's path to the `ca_certs` key in the pluginConfig");
                }

                if (status.Status != X509ChainStatusFlags.NoError && status.Status != X509ChainStatusFlags.UntrustedRoot)
                {
                    throw new ValidationException($"Failed to build the X509 chain due to {status.StatusInformation}");
                }
            }

            return chain.ChainElements.Select(x => x.Certificate.RawData).ToList();
        }
    }
}
