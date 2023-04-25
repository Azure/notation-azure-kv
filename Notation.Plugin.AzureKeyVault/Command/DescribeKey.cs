using System.Text.Json;
using Notation.Plugin.Protocol;
using Notation.Plugin.AzureKeyVault.Client;

namespace Notation.Plugin.AzureKeyVault.Command
{
    /// <summary>
    /// Implementation of describe-key command.
    /// </summary>
    public class DescribeKey : IPluginCommand
    {
        private DescribeKeyRequest _request;
        private IKeyVaultClient _keyVaultClient;
        private const string invalidInputError = "Invalid input. The valid input format is '{\"contractVersion\":\"1.0\",\"keyId\":\"https://<vaultname>.vault.azure.net/<keys|certificate>/<name>/<version>\"}'";

        /// <summary>
        /// Constructor to create DescribeKey object from JSON string.
        /// </summary>
        public DescribeKey(string inputJson)
        {
            // Deserialize JSON string to DescribeKeyRequest object
            var request = JsonSerializer.Deserialize<DescribeKeyRequest>(inputJson);
            if (request == null)
            {
                throw new ValidationException(invalidInputError);
            }
            this._request = request;
            this._keyVaultClient = new KeyVaultClient(request.KeyId);
        }

        /// <summary>
        /// Constructor for unit test.
        /// </summary>
        public DescribeKey(DescribeKeyRequest request, IKeyVaultClient keyVaultClient)
        {
            this._request = request;
            this._keyVaultClient = keyVaultClient;
        }

        public async Task<object> RunAsync()
        {
            // Get certificate from Azure Key Vault
            var cert = await _keyVaultClient.GetCertificateAsync();

            return new DescribeKeyResponse(
                keyId: _request.KeyId,
                keySpec: cert.KeySpec().EncodeKeySpec());
        }
    }
}
