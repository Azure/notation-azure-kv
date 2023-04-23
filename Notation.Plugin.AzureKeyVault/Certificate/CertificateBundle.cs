using System.Security.Cryptography.X509Certificates;

namespace Notation.Plugin.AzureKeyVault.Certificate
{
    /// <summary>
    /// Helper class to create a certificate bundle from a PEM file.
    /// </summary>
    static class CertificateBundle
    {
        /// <summary>
        /// Create a certificate bundle from a PEM file.
        /// </summary>
        public static X509Certificate2Collection Create(string pemFilePath)
        {
            var certificates = new X509Certificate2Collection();
            certificates.ImportFromPemFile(pemFilePath);
            return certificates;
        }
    }
}
