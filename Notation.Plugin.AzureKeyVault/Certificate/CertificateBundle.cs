using System.Security.Cryptography.X509Certificates;

namespace Notation.Plugin.AzureKeyVault.Certificate
{
    /// <summary>
    /// Helper class to create a certificate bundle from a PEM file.
    /// </summary>
    class CertificateBundle
    {
        /// <summary>
        /// Create a certificate bundle from a PEM file.
        /// </summary>
        public static X509Certificate2Collection Create(string pemFilePath)
        {
            // split the PEM file into certificates.
            string pemContent = File.ReadAllText(pemFilePath);
            string[] pemCertificates = pemContent.Split("-----END CERTIFICATE-----", StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries);

            // Add the certificates to the bundle.
            var certs = pemCertificates.Select(x => ConvertPemToDer(x))
                                       .Where(x => x != null && x.Length > 0)
                                       .Select(x => new X509Certificate2(x));
            return new X509Certificate2Collection(certs.ToArray());
        }

        /// <summary>
        /// Convert PEM to DER. It removes the header and footer of the PEM 
        /// file, merges multiple lines into one and decodes the base64 string.
        /// </summary>
        private static byte[] ConvertPemToDer(string pem)
        {
            // remove the header and footer of the PEM file.
            var lines = pem.Split('\n', StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries)
                           .Where(x => !x.StartsWith("-----"));

            // merge multiple lines into one.
            return Convert.FromBase64String(String.Join("", lines));
        }
    }
}
