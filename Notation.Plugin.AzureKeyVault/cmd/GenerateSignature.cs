using System.Text.Json;
using Notation.Plugin.Proto;
using Azure.Security.KeyVault.Keys.Cryptography;
using Notation.Plugin.AzureKeyVault.Certificate;

namespace Notation.Plugin.AzureKeyVault.Cmd
{
    /// <summary>
    /// Implementation of describe-key command.
    /// </summary>
    public class GenerateSignature : IPluginCommand
    {
        public async Task<object> RunAsync(string inputJson)
        {
            // parse the input
            GenerateSignatureRequest? input = JsonSerializer.Deserialize<GenerateSignatureRequest>(inputJson);
            if (input == null)
            {
                throw new ValidationException("Invalid input");
            }

            var akvClient = new AzureKeyVault(input.KeyId);

            // extract signature algorithm from the certificate
            var cert = await akvClient.GetCertificate();
            var keySpec = KeySpecUtils.ExtractKeySpec(cert);
            var signatureAlgorithm = SignatureAlgorithmHelper.FromKeySpec(keySpec);

            // sign
            var signature = await akvClient.Sign(signatureAlgorithm, input.Payload);

            List<byte[]> certificateChain = new List<byte[]>();
            if (input.PluginConfig != null && input.PluginConfig.ContainsKey("ca_certs"))
            {
                // build the entire certificate chain from the certificate 
                // bundle (including the intermediate and root certificates).
                var caCertsPath = input.PluginConfig["ca_certs"];
                certificateChain = CertificateChain.Build(CustomX509Store.Create(caCertsPath), cert);
            }
            else if (input.PluginConfig != null && input.PluginConfig.ContainsKey("as_secert"))
            {
                // read the entire certificate chain from the Azure Key Vault secret.
                throw new NotImplementedException("as_secret is not implemented yet");
            }
            else
            {
                // self-signed leaf certificate
                certificateChain.Add(cert.RawData);
            }

            return new GenerateSignatureResponse(
                keyId: input.KeyId,
                signature: signature,
                signingAlgorithm: KeySpecUtils.ToSigningAlgorithm(keySpec),
                certificateChain: certificateChain);
        }
    }
}