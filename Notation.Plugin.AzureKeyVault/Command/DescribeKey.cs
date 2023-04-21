using System.Text.Json;
using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault.Command
{
    /// <summary>
    /// Implementation of describe-key command.
    /// </summary>
    public class DescribeKey : IPluginCommand
    {
        private const string invalidInputError = "Invalid input. The valid input format is '{\"contractVersion\":\"1.0\",\"keyId\":\"https://<vaultname>.vault.azure.net/<keys|certificate>/<name>/<version>\"}'";

        public async Task<object> RunAsync(string inputJson)
        {
            // Deserialize JSON string to DescribeKeyRequest object
            DescribeKeyRequest? request = JsonSerializer.Deserialize<DescribeKeyRequest>(inputJson);
            if (request == null)
            {
                throw new ValidationException(invalidInputError);
            }

            // get certificate from Azure Key Vault
            var akvClient = new AzureKeyVault(request.KeyId);
            var cert = await akvClient.GetCertificate();

            // extract key spec from the certificate
            var keySpec = KeySpecUtils.ExtractKeySpec(cert);
            var encodedKeySpec = KeySpecUtils.EncodeKeySpec(keySpec);

            return new DescribeKeyResponse(
                keyId: request.KeyId,
                keySpec: encodedKeySpec);
        }
    }
}