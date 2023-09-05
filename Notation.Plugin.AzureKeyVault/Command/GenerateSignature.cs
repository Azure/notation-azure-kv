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

            // Sign
            var signature = await _keyVaultClient.SignAsync(keySpec.ToKeyVaultSignatureAlgorithm(), _request.Payload);

            return new GenerateSignatureResponse(
                keyId: _request.KeyId,
                signature: signature,
                signingAlgorithm: keySpec.ToSigningAlgorithm(),
                certificateChain: certChain.Select(x => x.RawData).ToList());
        }
    }
}
