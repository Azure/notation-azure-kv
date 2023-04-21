using System.Security.Cryptography.X509Certificates;
using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault.Certificate
{
    /// <summary>
    /// Helper class to build certificate chain.
    /// </summary>
    class CertificateChain
    {
        /// <summary>
        /// Build a certificate chain from a leaf certificate and a 
        /// certificate bundle.
        /// 
        /// <param name="certificateBundle">The certificate bundle.</param>
        /// <param name="leafCert">The leaf certificate.</param>
        /// <returns>a list of raw certificates in a chain.</returns>
        /// </summary>
        public static List<byte[]> Build(X509Certificate2Collection certificateBundle, X509Certificate2 leafCert)
        {
            X509Chain chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.AddRange(certificateBundle);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

            bool isValid = chain.Build(leafCert);
            if (!isValid)
            {
                throw new ValidationException("Certificate is invalid");
            }

            return chain.ChainElements.Select(x => x.Certificate.RawData).ToList();
        }
    }
}