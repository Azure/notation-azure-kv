using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Notation.Plugin.AzureKeyVault.Credential;
using Notation.Plugin.AzureKeyVault.Certificate;
using Notation.Plugin.AzureKeyVault.Client;
using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault.Command
{
    /// <summary>
    /// Implementation of generate-signature command.
    /// </summary>
    public class GenerateSignature : IPluginCommand
    {
        /// <summary>
        /// Plugin config key for specifying the RSA signing scheme.
        /// Supported values: "rsassa-pss" (default, for JWS/COSE) and
        /// "rsassa-pkcs1-v1_5" (for PKCS#7/dm-verity).
        /// </summary>
        public const string SigningSchemeConfigKey = "signing_scheme";

        private GenerateSignatureRequest _request;
        private IKeyVaultClient _keyVaultClient;

        /// <summary>
        /// Constructor to create GenerateSignature object from JSON string.
        /// </summary>
        public GenerateSignature(string inputJson)
        {
            // Parse the input
            var request = JsonSerializer.Deserialize(inputJson, GenerateSignatureRequestContext.Default.GenerateSignatureRequest);
            if (request == null)
            {
                throw new ValidationException("Failed to parse the request in JSON format. Please contact Notation maintainers to resolve the issue.");
            }
            this._request = request;
            this._keyVaultClient = new KeyVaultClient(
                id: request.KeyId,
                credential: Credentials.GetCredentials(request.PluginConfig));
        }

        /// <summary>
        /// Constructor for unit test.
        /// </summary>
        public GenerateSignature(GenerateSignatureRequest request, IKeyVaultClient keyVaultClient)
        {
            this._request = request;
            this._keyVaultClient = keyVaultClient;
        }

        public async Task<IPluginResponse> RunAsync()
        {
            // Obtain the certificate chain
            var certChain = new X509Certificate2Collection();
            X509Certificate2 leafCert;
            string? certBundlePath = _request.PluginConfig?.GetValueOrDefault("ca_certs", "");

            if (_request.PluginConfig?.GetValueOrDefault("self_signed")?.ToLower() == "true")
            {
                if (!string.IsNullOrEmpty(certBundlePath))
                {
                    throw new PluginException("Self-signed certificate is specified. Please do not specify the `ca_certs` parameter if it is a self-signed certificate.");
                }
                // Obtain self-signed leaf certificate from Azure Key Vault
                leafCert = await _keyVaultClient.GetCertificateAsync();
                certChain.Add(leafCert);
            }
            else
            {
                if (string.IsNullOrEmpty(certBundlePath))
                {
                    // Obtain the certificate chain from Azure Key Vault using 
                    // GetSecret permission. Ensure intermediate and root 
                    // certificates are merged into the Key Vault certificate to 
                    // retrieve the full chain.
                    // reference: https://learn.microsoft.com//azure/key-vault/certificates/create-certificate-signing-request
                    X509Certificate2Collection? certificateChain;
                    try
                    {
                        certificateChain = await _keyVaultClient.GetCertificateChainAsync();
                    }
                    catch (Azure.RequestFailedException ex)
                    {
                        if (ex.Message.Contains("does not have secrets get permission"))
                        {
                            throw new PluginException("The plugin does not have secrets get permission. Please grant the permission to the credential associated with the plugin or specify the file path of the certificate chain bundle through the `ca_certs` parameter in the plugin config.");
                        }
                        throw;
                    }

                    // build the certificate chain
                    certChain = CertificateChain.Build(certificateChain);
                    leafCert = certChain.First();
                }
                else
                {
                    // Obtain the certificate bundle from file 
                    // (including the intermediate and root certificates).
                    var certBundle = CertificateBundle.Create(certBundlePath);

                    // obtain the leaf certificate from Azure Key Vault
                    leafCert = await _keyVaultClient.GetCertificateAsync();

                    // build the certificate chain
                    certChain.Add(leafCert);
                    certChain.AddRange(certBundle);
                    certChain = CertificateChain.Build(certChain);
                }
            }

            // Extract KeySpec from the certificate
            var keySpec = leafCert.KeySpec();

            // Get the signing scheme from plugin config (default: rsassa-pss)
            // Supported values:
            //   - "rsassa-pss" (default): RSASSA-PSS padding for JWS/COSE signatures
            //   - "rsassa-pkcs1-v1_5": RSASSA-PKCS1-v1_5 padding for PKCS#7/dm-verity signatures
            string? signingScheme = _request.PluginConfig?.GetValueOrDefault(SigningSchemeConfigKey);

            // For RSASSA-PKCS1-v1_5 + EC, fail fast: PKCS#7-based verifiers
            // cannot consume ECDSA signatures.
            if (string.Equals(signingScheme, SigningScheme.RSASSA_PKCS1_V1_5, StringComparison.OrdinalIgnoreCase)
                && keySpec.Type == KeyType.EC)
            {
                throw new ValidationException(
                    $"Signing scheme '{SigningScheme.RSASSA_PKCS1_V1_5}' requires an RSA key; got EC.");
            }

            // Determine the Azure Key Vault signature algorithm based on scheme
            var akvAlgorithm = keySpec.ToKeyVaultSignatureAlgorithm(signingScheme);

            // Sign using the selected algorithm
            var signature = await _keyVaultClient.SignAsync(akvAlgorithm, _request.Payload);

            // Determine the notation signing algorithm string based on scheme
            var signingAlgorithm = keySpec.ToSigningAlgorithm(signingScheme);

            return new GenerateSignatureResponse(
                keyId: _request.KeyId,
                signature: signature,
                signingAlgorithm: signingAlgorithm,
                certificateChain: certChain.Select(x => x.RawData).ToList());
        }
    }
}
