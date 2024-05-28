using System.Text.Json;
using Notation.Plugin.AzureKeyVault.Client;
using Notation.Plugin.AzureKeyVault.Credential;
using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault.Command
{
    /// <summary>
    /// Implementation of describe-key command.
    /// </summary>
    public class DescribeKey : IPluginCommand
    {
        private DescribeKeyRequest _request;
        private IKeyVaultClient _keyVaultClient;

        /// <summary>
        /// Constructor to create DescribeKey object from JSON string.
        /// </summary>
        public DescribeKey(string inputJson)
        {
            // Deserialize JSON string to DescribeKeyRequest object
            var request = JsonSerializer.Deserialize(inputJson, DescribeKeyRequestContext.Default.DescribeKeyRequest);
            if (request == null)
            {
                throw new ValidationException("Failed to parse the request in JSON format. Please contact Notation maintainers to resolve the issue.");
            }
            this._request = request;
            this._keyVaultClient = new KeyVaultClient(
                id: request.KeyId,
                credential: Credentials.GetCredentials(request.PluginConfig));
        }

        /// <summary>
        /// Constructor for unit test.
        /// </summary>
        public DescribeKey(DescribeKeyRequest request, IKeyVaultClient keyVaultClient)
        {
            this._request = request;
            this._keyVaultClient = keyVaultClient;
        }

        public async Task<IPluginResponse> RunAsync()
        {
            // Get certificate from Azure Key Vault
            var cert = await _keyVaultClient.GetCertificateAsync();

            return new DescribeKeyResponse(
                keyId: _request.KeyId,
                keySpec: cert.KeySpec().EncodeKeySpec());
        }
    }
}
