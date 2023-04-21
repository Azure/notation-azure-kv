using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Notation.Plugin.AzureKeyVault.Certificate
{
    class CertificateBundle
    {
        /// <summary>
        /// Create a certificate bundle from a PEM file.
        /// </summary>
        public static X509Certificate2Collection Create(string pemFilePath)
        {
            // split the PEM file into certificates.
            string pemContent = File.ReadAllText(pemFilePath);
            string[] pemCertificates = pemContent.Split("-----END CERTIFICATE-----", StringSplitOptions.RemoveEmptyEntries);

            // Add the certificates to the bundle.
            X509Certificate2Collection certBundle = new X509Certificate2Collection();
            foreach (string pemCertificate in pemCertificates)
            {
                byte[] rawCert = ConvertPemToDer(pemCertificate);
                if (rawCert != null && rawCert.Length > 0)
                {
                    certBundle.Add(new X509Certificate2(rawCert));
                }
            }
            return certBundle;
        }

        /// <summary>
        /// Convert PEM to DER. It removes the header and footer of the PEM 
        /// file, merges multiple lines into one and decodes the base64 string.
        /// </summary>
        private static byte[] ConvertPemToDer(string pem)
        {
            StringBuilder builder = new StringBuilder();

            // remove the header and footer of the PEM file.
            var lines = pem.Split('\n').Where(x => !x.StartsWith("-----") && !string.IsNullOrWhiteSpace(x));

            // merge multiple lines into one.
            return Convert.FromBase64String(String.Join("", lines));
        }
    }
}
