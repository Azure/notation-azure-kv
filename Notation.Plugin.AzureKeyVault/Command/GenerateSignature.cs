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
        public async Task<object> RunAsync(string inputJson)
        {
            // Parse the input
            GenerateSignatureRequest? input = JsonSerializer.Deserialize<GenerateSignatureRequest>(inputJson);
            if (input == null)
            {
                throw new ValidationException("Invalid input");
            }

            var akvClient = new KeyVaultClient(input.KeyId);

            // Extract signature algorithm from the certificate
            var leafCert = await akvClient.GetCertificate();
            var keySpec = leafCert.KeySpec();
            var signatureAlgorithm = keySpec.ToSignatureAlgorithm();

            // Sign
            var signature = await akvClient.Sign(signatureAlgorithm, input.Payload);

            // Build the certificate chain
            List<byte[]> certificateChain = new List<byte[]>();
            X509Certificate2Collection certBundle = new X509Certificate2Collection();
            if (input.PluginConfig?.ContainsKey("ca_certs") ?? false)
            {
                // Build the certificate chain from the certificate 
                // bundle (including the intermediate and root certificates).
                certBundle = CertificateBundle.Create(input.PluginConfig["ca_certs"]);
            }
            else if (input.PluginConfig?.GetValueOrDefault("as_secret")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                // Obtain the certificate chain from Azure Key Vault using 
                // GetSecret permission. Ensure intermediate and root 
                // certificates are merged into the Key Vault certificate to 
                // retrieve the full chain.
                // reference: https://learn.microsoft.com//azure/key-vault/certificates/create-certificate-signing-request
                certBundle = await akvClient.GetCertificateChain();
            }

            return new GenerateSignatureResponse(
                keyId: input.KeyId,
                signature: signature,
                signingAlgorithm: keySpec.ToSigningAlgorithm(),
                certificateChain: CertificateChain.Build(leafCert, certBundle));
        }
    }
}
