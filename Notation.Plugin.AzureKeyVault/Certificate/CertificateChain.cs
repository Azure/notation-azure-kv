using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
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

        /// <summary>
        /// Decode a PKCS12 certificate bundle.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="ValidationException"></exception>
        public static X509Certificate2Collection DecodeFromPKCS12(byte[] data)
        {
            var chain = new X509Certificate2Collection();
            int consumed;
            Pkcs12Info pfx = Pkcs12Info.Decode(data, out consumed);
            if (consumed == 0)
            {
                throw new ValidationException($"Invalid PKCS12 content");
            }
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
                foreach (var bag in authSafe.GetBags())
                {
                    if (bag.GetType() != typeof(Pkcs12CertBag))
                    {
                        continue;
                    }
                    Pkcs12CertBag certBag = (Pkcs12CertBag)bag;
                    chain.Add(certBag.GetCertificate());
                }
            }
            return chain;
        }
    }
}
