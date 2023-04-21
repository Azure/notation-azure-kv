using System.Security.Cryptography.X509Certificates;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys.Cryptography;
using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault.Client
{
    class KeyVaultClient
    {
        // Azure Key Vault URL (e.g. https://<vaultname>.vault.azure.net)
        private string keyVaultUrl;
        // Key name or certificate name
        private string name;
        // Key version or certificate version
        private string version;
        // Key identifier (e.g. https://<vaultname>.vault.azure.net/keys/<name>/<version>)
        private string keyId;
        // Certificate client (lazy initialization)
        private Lazy<CertificateClient> _certificateClient;
        // Cryptography client (lazy initialization)
        private Lazy<CryptographyClient> _cryptoClient;

        private const string invalidInputErrorMessage = "Invalid input. The valid input format is '{\"contractVersion\":\"1.0\",\"keyId\":\"https://<vaultname>.vault.azure.net/<keys|certificate>/<name>/<version>\"}'";

        /// <summary>
        /// Constructor to create AzureKeyVault object from key identifier or
        /// certificate identifier.
        /// </summary>
        public KeyVaultClient(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id), "Id must not be null or empty");
            }

            // Example uri: https://notationakvtest.vault.azure.net/keys/notationev10leafcert/847956cbd58c4937ab04d8ab8622000c
            var uri = new Uri(id);

            // Validate uri
            if (uri.Segments.Length != 4)
            {
                throw new ValidationException(invalidInputErrorMessage);
            }
            if (uri.Segments[1] != "keys/" && uri.Segments[1] != "certificates/")
            {
                throw new ValidationException(invalidInputErrorMessage);
            }
            if (uri.Scheme != "https")
            {
                throw new ValidationException(invalidInputErrorMessage);
            }

            // Extract keys|certificates name from the uri
            this.keyVaultUrl = $"{uri.Scheme}://{uri.Host}";
            this.name = uri.Segments[2].TrimEnd('/');
            this.version = uri.Segments[3].TrimEnd('/');
            this.keyId = $"{keyVaultUrl}/keys/{name}/{version}";

            // initialize credential and lazy clients
            var credential = new DefaultAzureCredential();
            this._certificateClient = new Lazy<CertificateClient>(() => new CertificateClient(new Uri(keyVaultUrl), credential));
            this._cryptoClient = new Lazy<CryptographyClient>(() => new CryptographyClient(new Uri(keyId), credential));
        }

        /// <summary>
        /// Constructor to create AzureKeyVault object from keyVaultUrl, name 
        /// and version.
        /// </summary>
        public KeyVaultClient(string keyVaultUrl, string name, string version) : this($"{keyVaultUrl}/keys/{name}/{version}") { }

        /// <summary>
        /// Sign the payload and return the signature.
        /// </summary>
        public async Task<byte[]> Sign(SignatureAlgorithm algorithm, byte[] payload)
        {
            var signResult = await _cryptoClient.Value.SignDataAsync(algorithm, payload);

            if (signResult.KeyId != keyId)
            {
                throw new PluginException("Invalid keys or certificates identifier.");
            }

            if (signResult.Algorithm != algorithm)
            {
                throw new PluginException("Invalid signature algorithm.");
            }

            return signResult.Signature;
        }

        /// <summary>
        /// Get the certificate from the key vault.
        /// </summary>
        public async Task<X509Certificate2> GetCertificate()
        {
            var cert = await _certificateClient.Value.GetCertificateVersionAsync(name, version);

            // If the version is invalid, the cert will be fallback to 
            // the latest. So if the version is not the same as the
            // requested version, it means the version is invalid.
            if (cert.Value.Properties.Version != version)
            {
                throw new ValidationException("Invalid certificate version.");
            }

            return new X509Certificate2(cert.Value.Cer);
        }
    }
}
