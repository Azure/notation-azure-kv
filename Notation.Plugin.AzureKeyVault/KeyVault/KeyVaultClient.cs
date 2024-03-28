using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Azure.Core;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Secrets;
using Notation.Plugin.AzureKeyVault.Certificate;
using Notation.Plugin.Protocol;

[assembly: InternalsVisibleTo("Notation.Plugin.AzureKeyVault.Tests")]
namespace Notation.Plugin.AzureKeyVault.Client
{
    public interface IKeyVaultClient
    {
        /// <summary>
        /// Sign the payload with the specified algorithm.
        /// </summary>
        public Task<byte[]> SignAsync(SignatureAlgorithm algorithm, byte[] payload);

        /// <summary>
        /// Get the certificate from KeyVault.
        /// </summary>
        public Task<X509Certificate2> GetCertificateAsync();

        /// <summary>
        /// Get the certificate chain from KeyVault.
        /// </summary>
        public Task<X509Certificate2Collection> GetCertificateChainAsync();
    }

    public class KeyVaultClient : IKeyVaultClient
    {
        /// <summary>
        /// A helper record to store KeyVault metadata.
        /// </summary>
        private record KeyVaultMetadata(string KeyVaultUrl, string Name, string Version);

        // Certificate client (lazy initialization)
        // Protected for unit test
        protected Lazy<CertificateClient> _certificateClient;
        // Cryptography client (lazy initialization)
        protected Lazy<CryptographyClient> _cryptoClient;
        // Secret client (lazy initialization)
        protected Lazy<SecretClient> _secretClient;
        // Error message for invalid input
        private const string INVALID_INPUT_ERROR_MSG = "Invalid input. The valid input format is '{\"contractVersion\":\"1.0\",\"keyId\":\"https://<vaultname>.vault.azure.net/<keys|certificate>/<name>/<version>\"}'";

        // Key name or certificate name
        private string _name;
        // Key version or certificate version
        private string _version;
        // Key identifier (e.g. https://<vaultname>.vault.azure.net/keys/<name>/<version>)
        private string _keyId;

        // Internal getters for unit test
        internal string Name => _name;
        internal string Version => _version;
        internal string KeyId => _keyId;

        /// <summary>
        /// Constructor to create AzureKeyVault object from keyVaultUrl, name 
        /// and version.
        /// </summary>
        public KeyVaultClient(string keyVaultUrl, string name, string version, TokenCredential credential)
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

            this._name = name;
            this._version = version;
            this._keyId = $"{keyVaultUrl}/keys/{name}/{version}";

            // initialize credential and lazy clients
            this._certificateClient = new Lazy<CertificateClient>(() => new CertificateClient(new Uri(keyVaultUrl), credential));
            this._cryptoClient = new Lazy<CryptographyClient>(() => new CryptographyClient(new Uri(_keyId), credential));
            this._secretClient = new Lazy<SecretClient>(() => new SecretClient(new Uri(keyVaultUrl), credential));
        }

        /// <summary>
        /// Constructor to create AzureKeyVault object from key identifier or
        /// certificate identifier.
        /// </summary>
        /// <param name="id">
        /// Key identifier or certificate identifier. (e.g. https://<vaultname>.vault.azure.net/keys/<name>/<version>)
        /// </param>
        /// <param name="credential">
        /// TokenCredential object to authenticate with Azure Key Vault.
        /// </param>
        public KeyVaultClient(string id, TokenCredential credential) : this(ParseId(id), credential) { }

        /// <summary>
        /// A helper constructor to create KeyVaultClient from KeyVaultMetadata.
        /// </summary>
        private KeyVaultClient(KeyVaultMetadata metadata, TokenCredential credential)
            : this(metadata.KeyVaultUrl, metadata.Name, metadata.Version, credential) { }

        /// <summary>
        /// A helper function to parse key identifier or certificate identifier
        /// and return KeyVaultMetadata.
        /// </summary>
        private static KeyVaultMetadata ParseId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id), "Id must not be null or empty");
            }

            var uri = new Uri(id);
            // Validate uri
            if (uri.Segments.Length != 4)
            {
                throw new ValidationException(INVALID_INPUT_ERROR_MSG);
            }

            if (uri.Segments[1] != "keys/" && uri.Segments[1] != "certificates/")
            {
                throw new ValidationException(INVALID_INPUT_ERROR_MSG);
            }

            if (uri.Scheme != "https")
            {
                throw new ValidationException(INVALID_INPUT_ERROR_MSG);
            }

            return new KeyVaultMetadata(
                KeyVaultUrl: $"{uri.Scheme}://{uri.Host}",
                Name: uri.Segments[2].TrimEnd('/'),
                Version: uri.Segments[3].TrimEnd('/')
            );
        }

        /// <summary>
        /// Sign the payload and return the signature.
        /// </summary>
        public async Task<byte[]> SignAsync(SignatureAlgorithm algorithm, byte[] payload)
        {
            var signResult = await _cryptoClient.Value.SignDataAsync(algorithm, payload);
            if (signResult.KeyId != _keyId)
            {
                throw new PluginException($"Invalid keys identifier. The user provides {_keyId} but the response contains {signResult.KeyId} as the keys");
            }

            if (signResult.Algorithm != algorithm)
            {
                throw new PluginException($"Invalid signature algorithm. The user provides {algorithm} but the response contains {signResult.Algorithm} as the algorithm");
            }

            return signResult.Signature;
        }

        /// <summary>
        /// Get the certificate from the key vault.
        /// </summary>
        public async Task<X509Certificate2> GetCertificateAsync()
        {
            var cert = await _certificateClient.Value.GetCertificateVersionAsync(_name, _version);

            // If the version is invalid, the cert will be fallback to 
            // the latest. So if the version is not the same as the
            // requested version, it means the version is invalid.
            if (cert.Value.Properties.Version != _version)
            {
                throw new ValidationException($"Invalid certificate version. The user provides {_version} but the response contains {cert.Value.Properties.Version} as the version");
            }

            return new X509Certificate2(cert.Value.Cer);
        }

        /// <summary>
        /// Get the certificate chain from the key vault with GetSecret permission.
        /// </summary>
        public async Task<X509Certificate2Collection> GetCertificateChainAsync()
        {
            var secret = await _secretClient.Value.GetSecretAsync(_name, _version);

            var chain = new X509Certificate2Collection();
            var contentType = secret.Value.Properties.ContentType;
            var secretValue = secret.Value.Value;
            switch (contentType)
            {
                case "application/x-pkcs12":
                    // If the secret is a PKCS12 file, decode the base64 encoding
                    // Import will reverse the order of the certificates 
                    // in the chain
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        // macOS doesn't support non-encrypted MAC
                        // https://github.com/dotnet/runtime/issues/23635
                        chain.Import(
                            rawData: Pkcs12.ReEncode(Convert.FromBase64String(secretValue)),
                            password: null,
                            keyStorageFlags: X509KeyStorageFlags.DefaultKeySet);
                    }
                    else
                    {
                        chain.Import(
                            rawData: Convert.FromBase64String(secretValue),
                            password: null,
                            keyStorageFlags: X509KeyStorageFlags.EphemeralKeySet);
                    }
                    break;
                case "application/x-pem-file":
                    // If the secret is a PEM file, parse the PEM content directly
                    chain.ImportFromPem(secretValue.ToCharArray());
                    break;
                default:
                    throw new ValidationException($"Unsupported secret content type: {contentType}");
            }
            return chain;
        }
    }
}
