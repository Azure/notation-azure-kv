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
        /// Build the certificate chain from the certificates by matching
        /// the issuer and subject distinguished name.
        /// 
        /// Note: It doesn't verify the certificate path.
        /// </summary>
        /// <param name="certs"></param>
        /// <returns>
        /// The certificate chain. The order is from leaf to root.
        /// </returns>
        /// <exception cref="PluginException"></exception>
        public static X509Certificate2Collection Build(X509Certificate2Collection certs)
        {
            /*
            Valid case:
            1. self-signed leaf cert
            2. cert chain: CA1 -> CA2 -> leaf

            Invalid cases:
            1. empty
            2. non-self-signed leaf cert
            3. missing intermediate cert: CA1 -> (x) -> leaf 
            4. missing root cert: (x) -> CA2 -> leaf
            5. unnecessary intermediate cert: CA1 -> CA2 -> leaf
                                              (x) -> CA3
            6. unnecessary root: CA1 -> CA2 -> leaf
                                 CA3
            6. unnecessary cert: CA1 -> CA2 -> leaf
                                 (x) -> CA3
            7. duplicated certs: CA1 -> CA2 -> leaf
            8. multiple leaf certs: CA1 -> CA2 -> leaf1
                                            └───> leaf2
            9. multiple chain: CA1 -> CA2 -> leaf1
                              CA3 -> leaf2
            10. DN cycle: CA1 -> CA2 -> leaf1
                           ^      |
                           └──────┘
            */
            if (certs.Count == 0)
            {
                throw new PluginException("The certificates that need to be built into a chain are empty.");
            }

            if (certs.Count == 1)
            {
                if (certs[0].SubjectName.Name != certs[0].IssuerName.Name)
                {
                    throw new PluginException("Only obtained one certificate but it is not a self-signed certificate. Please complete the certificate bundle by `ca_certs` through plugin config.");
                }
                return certs;
            }

            // subject distinguished name -> certificate map
            var certMap = new Dictionary<string, X509Certificate2>();
            var caSet = new HashSet<string>();
            foreach (var cert in certs)
            {
                if (certMap.ContainsKey(cert.SubjectName.Name))
                {
                    throw new PluginException($"Found duplicated certificates: {cert.SubjectName.Name}");
                }
                certMap[cert.SubjectName.Name] = cert;
                caSet.Add(cert.IssuerName.Name);
            }

            // count the leaf certificate
            if (certs.Count(x => !caSet.Contains(x.SubjectName.Name)) != 1)
            {
                // AKV certificates always contain the leaf certificate
                throw new PluginException("Found multiple leaf certificates or unreferenced intermediate CAs");
            }
            var leafCert = certs.First(x => !caSet.Contains(x.SubjectName.Name));

            // build the certificate chain
            X509Certificate2Collection chain = new X509Certificate2Collection();
            var currentCert = leafCert;
            while (true)
            {
                chain.Add(currentCert);
                var subjectDN = currentCert.SubjectName.Name;
                var issuerDN = currentCert.IssuerName.Name;

                if (isRootCA(currentCert))
                {
                    break;
                }

                if (!caSet.Remove(issuerDN))
                {
                    throw new PluginException($"Found multiple certificates issued by {issuerDN}");
                }

                // check if the issuer is found in the certificate bundle
                if (!certMap.TryGetValue(issuerDN, out currentCert))
                {
                    throw new PluginException($"Certificate chain is not complete. The issuer of {subjectDN} is not found.");
                }
            }

            if (chain.Count != certs.Count)
            {
                throw new PluginException($"Obtained {certs.Count} certificates but the certificate chain only needs {chain.Count} certficates.");
            }
            return chain;
        }

        /// <summary>
        /// Check if the certificate is a root CA.
        /// </summary>
        /// <param name="cert"></param>
        /// <returns></returns>
        public static bool isRootCA(X509Certificate2 cert)
        {
            return cert.SubjectName.Name == cert.IssuerName.Name;
        }
    }
}
