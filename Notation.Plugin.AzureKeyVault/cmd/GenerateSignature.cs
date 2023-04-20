using System.Text.Json;
using Notation.Plugin.Proto;
using Azure.Security.KeyVault.Keys.Cryptography;

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
            // TODO - get the certificate chain

            return new GenerateSignatureResponse(
                keyId: input.KeyId,
                signature: signature,
                signingAlgorithm: KeySpecUtils.ToSigningAlgorithm(keySpec),
                certificateChain: new List<byte[]> { cert.RawData });
        }
    }
}