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
            var request = JsonSerializer.Deserialize<GenerateSignatureRequest>(inputJson);
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

        public async Task<object> RunAsync()
        {
            // Extract signature algorithm from the certificate
            var leafCert = await _keyVaultClient.GetCertificateAsync();
            var keySpec = leafCert.KeySpec();
            var signatureAlgorithm = keySpec.ToSignatureAlgorithm();

            // Sign
            var signature = await _keyVaultClient.SignAsync(signatureAlgorithm, _request.Payload);

            // Build the certificate chain
            List<byte[]> certificateChain = new List<byte[]>();
            if (_request.PluginConfig?.ContainsKey("ca_certs") == true)
            {
                // Build the entire certificate chain from the certificate 
                // bundle (including the intermediate and root certificates).
                var caCertsPath = _request.PluginConfig["ca_certs"];
                certificateChain = CertificateChain.Build(leafCert, CertificateBundle.Create(caCertsPath));
            }
            else if (_request.PluginConfig?.ContainsKey("as_secret") == true)
            {
                // Read the entire certificate chain from the Azure Key Vault with GetSecret permission.
                throw new NotImplementedException("as_secret is not implemented yet");
            }
            else
            {
                // validate the self-signed leaf certificate
                certificateChain = CertificateChain.Build(leafCert, new X509Certificate2Collection());
            }

            return new GenerateSignatureResponse(
                keyId: _request.KeyId,
                signature: signature,
                signingAlgorithm: keySpec.ToSigningAlgorithm(),
                certificateChain: certificateChain);
        }
    }
}
