using System.Security.Cryptography.X509Certificates;

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
        /// Note: the method doen't check the validity of the chain.
        /// <param name="certificateBundle">The certificate bundle.</param>
        /// <param name="leafCert">The leaf certificate.</param>
        /// <returns>A list of raw certificates in a chain.</returns>
        /// </summary>
        public static List<byte[]> Build(X509Certificate2 leafCert, X509Certificate2Collection certificateBundle)
        {
            X509Certificate2Collection chain = new X509Certificate2Collection(leafCert);
            chain.AddRange(certificateBundle);
            return chain.Select(x => x.RawData).ToList();
        }
    }
}
