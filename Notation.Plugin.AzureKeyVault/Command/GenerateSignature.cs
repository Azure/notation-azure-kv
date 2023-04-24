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

            // Obtain the certificate chain
            X509Certificate2Collection certBundle;
            X509Certificate2 leafCert;
            if (input.PluginConfig?.ContainsKey("ca_certs") == true)
            {
                // Obtain the certificate bundle from file 
                // (including the intermediate and root certificates).
                certBundle = CertificateBundle.Create(input.PluginConfig["ca_certs"]);

                // obtain the leaf certificate from Azure Key Vault
                leafCert = await akvClient.GetCertificateAsync();
            }
            else if (input.PluginConfig?.GetValueOrDefault("as_secret")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Obtain the certificate chain from Azure Key Vault using 
                // GetSecret permission. Ensure intermediate and root 
                // certificates are merged into the Key Vault certificate to 
                // retrieve the full chain.
                // reference: https://learn.microsoft.com//azure/key-vault/certificates/create-certificate-signing-request
                var certificateChain = await akvClient.GetCertificateChainAsync();

                // the certBundle is the certificates start from the second one of certificateChain
                certBundle = new X509Certificate2Collection(certificateChain.Skip(1).ToArray());

                // the leafCert is the first certificate in the certBundle
                leafCert = certificateChain[0];
            }
            else
            {
                // only have the leaf certificate
                certBundle = new X509Certificate2Collection();
                leafCert = await akvClient.GetCertificateAsync();
            }

            // Extract KeySpec from the certificate
            var keySpec = leafCert.KeySpec();

            // Sign
            var signature = await akvClient.SignAsync(keySpec.ToSignatureAlgorithm(), input.Payload);

            return new GenerateSignatureResponse(
                keyId: input.KeyId,
                signature: signature,
                signingAlgorithm: keySpec.ToSigningAlgorithm(),
                certificateChain: CertificateChain.Build(leafCert, certBundle));
        }
    }
}
