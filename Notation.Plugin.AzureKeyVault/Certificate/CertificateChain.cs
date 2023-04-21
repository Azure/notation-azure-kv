using System.Security.Cryptography.X509Certificates;
using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault.Certificate
{
    class CertificateChain
    {
        /// <summary>
        /// Build a certificate chain from a leaf certificate and a custom X509 
        /// store.
        /// </summary>
        public static List<byte[]> Build(X509Store store, X509Certificate2 leafCert)
        {
            X509Chain chain = new X509Chain();
            chain.ChainPolicy.ExtraStore.AddRange(store.Certificates);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

            bool isValid = chain.Build(leafCert);
            if (!isValid)
            {
                throw new ValidationException("Certificate is invalid");
            }

            List<byte[]> chainBytesList = new List<byte[]>();
            foreach (X509ChainElement chainElement in chain.ChainElements)
            {
                chainBytesList.Add(chainElement.Certificate.RawData);
            }

            // TODO - Clean up and close the custom store
            store.Close();
            return chainBytesList;
        }
    }
}