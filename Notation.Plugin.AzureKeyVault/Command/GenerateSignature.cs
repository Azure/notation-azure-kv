using System.Text.Json;
using Notation.Plugin.Protocol;
using Notation.Plugin.AzureKeyVault.Certificate;
using Notation.Plugin.AzureKeyVault.Client;

namespace Notation.Plugin.AzureKeyVault.Command
{
    /// <summary>
    /// Implementation of describe-key command.
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
            var keySpec = KeySpecUtils.ExtractKeySpec(leafCert);
            var signatureAlgorithm = SignatureAlgorithmHelper.FromKeySpec(keySpec);

            // Sign
            var signature = await akvClient.Sign(signatureAlgorithm, input.Payload);

            // Build the certificate chain
            List<byte[]> certificateChain = new List<byte[]>();
            if (input.PluginConfig != null && input.PluginConfig.ContainsKey("ca_certs"))
            {
                // Build the entire certificate chain from the certificate 
                // bundle (including the intermediate and root certificates).
                var caCertsPath = input.PluginConfig["ca_certs"];
                certificateChain = CertificateChain.Build(CertificateBundle.Create(caCertsPath), leafCert);
            }
            else if (input.PluginConfig != null && input.PluginConfig.ContainsKey("as_secret"))
            {
                // Read the entire certificate chain from the Azure Key Vault with GetSecret permission.
                throw new NotImplementedException("as_secret is not implemented yet");
            }
            else
            {
                // Self-signed leaf certificate
                certificateChain.Add(leafCert.RawData);
            }

            return new GenerateSignatureResponse(
                keyId: input.KeyId,
                signature: signature,
                signingAlgorithm: KeySpecUtils.ToSigningAlgorithm(keySpec),
                certificateChain: certificateChain);
        }
    }
}
