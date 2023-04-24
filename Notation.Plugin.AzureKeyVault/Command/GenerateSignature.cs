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
            var leafCert = await akvClient.GetCertificateAsync();
            var keySpec = leafCert.KeySpec();
            var signatureAlgorithm = keySpec.ToSignatureAlgorithm();

            // Sign
            var signature = await akvClient.SignAsync(signatureAlgorithm, input.Payload);

            // Build the certificate chain
            List<byte[]> certificateChain = new List<byte[]>();
            if (input.PluginConfig?.ContainsKey("ca_certs") == true)
            {
                // Build the entire certificate chain from the certificate 
                // bundle (including the intermediate and root certificates).
                var caCertsPath = input.PluginConfig["ca_certs"];
                certificateChain = CertificateChain.Build(leafCert, CertificateBundle.Create(caCertsPath));
            }
            else if (input.PluginConfig?.ContainsKey("as_secret") == true)
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
                keyId: input.KeyId,
                signature: signature,
                signingAlgorithm: keySpec.ToSigningAlgorithm(),
                certificateChain: certificateChain);
        }
    }
}
