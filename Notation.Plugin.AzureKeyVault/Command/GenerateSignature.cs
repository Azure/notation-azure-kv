using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
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
                throw new ValidationException("Invalid input");
            }
            this._request = request;
            this._keyVaultClient = new KeyVaultClient(request.KeyId);
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
            X509Certificate2Collection certBundle;
            X509Certificate2 leafCert;
            if (_request.PluginConfig?.TryGetValue("ca_certs", out var certBundlePath) == true)
            {
                // Obtain the certificate bundle from file 
                // (including the intermediate and root certificates).
                certBundle = CertificateBundle.Create(certBundlePath);

                // obtain the leaf certificate from Azure Key Vault
                leafCert = await _keyVaultClient.GetCertificateAsync();
            }
            else if (_request.PluginConfig?.TryGetValue("as_secret", out var asSecret) == true && asSecret.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                // Obtain the certificate chain from Azure Key Vault using 
                // GetSecret permission. Ensure intermediate and root 
                // certificates are merged into the Key Vault certificate to 
                // retrieve the full chain.
                // reference: https://learn.microsoft.com//azure/key-vault/certificates/create-certificate-signing-request
                var certificateChain = await _keyVaultClient.GetCertificateChainAsync();

                // the certBundle is the certificates start from the second one of certificateChain
                certBundle = new X509Certificate2Collection(certificateChain.Skip(1).ToArray());

                // the leafCert is the first certificate in the certBundle
                leafCert = certificateChain[0];
            }
            else
            {
                // only have the leaf certificate
                certBundle = new X509Certificate2Collection();
                leafCert = await _keyVaultClient.GetCertificateAsync();
            }

            // Extract KeySpec from the certificate
            var keySpec = leafCert.KeySpec();

            // Sign
            var signature = await _keyVaultClient.SignAsync(keySpec.ToKeyVaultSignatureAlgorithm(), _request.Payload);

            return new GenerateSignatureResponse(
                keyId: _request.KeyId,
                signature: signature,
                signingAlgorithm: keySpec.ToSigningAlgorithm(),
                certificateChain: CertificateChain.Build(leafCert, certBundle));
        }
    }
}
