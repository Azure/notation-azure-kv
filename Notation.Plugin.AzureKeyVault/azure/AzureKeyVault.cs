using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys.Cryptography;
using Notation.Plugin.Proto;

namespace Notation.Plugin.AzureKeyVault
{
    class AzureKeyVault
    {
        private string keyVaultUrl;
        private string name;
        private string version;
        private string id;

        private const string invalidInputError = "Invalid input. The valid input format is '{\"contractVersion\":\"1.0\",\"keyId\":\"https://<vaultname>.vault.azure.net/<keys|certificate>/<name>/<version>\"}'";

        /// <summary>
        /// Constructor to create AzureKeyVault object from keyVaultUrl, name 
        /// and version.
        /// </summary>
        public AzureKeyVault(string keyVaultUrl, string name, string version)
        {
            if (string.IsNullOrEmpty(keyVaultUrl))
            {
                throw new ArgumentNullException(nameof(keyVaultUrl), "KeyVaultUrl must not be null or empty");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), "KeyName must not be null or empty");
            }

            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentNullException(nameof(version), "KeyVersion must not be null or empty");
            }

            this.keyVaultUrl = keyVaultUrl;
            this.name = name;
            this.version = version;
            this.id = $"{keyVaultUrl}/keys/{name}/{version}";
        }

        /// <summary>
        /// Constructor to create AzureKeyVault object from key identifier or
        /// certificate identifier.
        /// </summary>
        public AzureKeyVault(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id), "Id must not be null or empty");
            }

            // example uri: https://notationakvtest.vault.azure.net/keys/notationev10leafcert/847956cbd58c4937ab04d8ab8622000c
            var uri = new Uri(id);

            // validate uri
            if (uri.Segments.Length != 4)
            {
                throw new ValidationException(invalidInputError);
            }
            if (uri.Segments[1] != "keys/" && uri.Segments[1] != "certificates/")
            {
                throw new ValidationException(invalidInputError);
            }
            if (uri.Scheme != "https")
            {
                throw new ValidationException(invalidInputError);
            }
            // extract keys|certificates name from the uri
            this.keyVaultUrl = $"{uri.Scheme}://{uri.Host}";
            this.name = uri.Segments[2].TrimEnd('/');
            this.version = uri.Segments[3].TrimEnd('/');
            this.id = id;
        }


        /// <summary>
        /// Sign the payload and return the signature.
        /// </summary>
        public async Task<byte[]> Sign(byte[] payload, SignatureAlgorithm algorithm)
        {
            var cryptoClient = new CryptographyClient(new Uri(id), new DefaultAzureCredential());
            var signature = await cryptoClient.SignDataAsync(SignatureAlgorithm.RS256, payload);
            return signature.Signature;
        }


        /// <summary>
        /// Get the certificate from the key vault.
        /// </summary>
        public async Task<X509Certificate2> GetCertificate()
        {
            var certificateClient = new CertificateClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
            var cert = await certificateClient.GetCertificateVersionAsync(name, version);
            
            // if the version is invalid, the cert will be fallback to 
            // the latest. So if the version is not the same as the
            // requested version, it means the version is invalid.
            if (cert.Value.Properties.Version != version){
                throw new ValidationException("Invalid certificate version.");
            } 
            return new X509Certificate2(cert.Value.Cer);
        }
    }
}