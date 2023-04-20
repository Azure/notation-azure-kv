using System.Text.Json.Serialization;

namespace Notation.Plugin.Proto
{
    /// <summary>
    /// Request class for describe-key command.
    /// </summary>
    public class DescribeKeyRequest
    {
        [JsonPropertyName("contractVersion")]
        public string ContractVersion { get; }

        [JsonPropertyName("keyId")]
        public string KeyId { get; }

        [JsonPropertyName("pluginConfig")]
        public Dictionary<string, string>? PluginConfig { get; set; }

        public DescribeKeyRequest(string contractVersion, string keyId)
        {
            if (string.IsNullOrEmpty(contractVersion))
            {
                throw new ArgumentNullException(nameof(contractVersion), "ContractVersion must not be null or empty");
            }

            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentNullException(nameof(keyId), "KeyId must not be null or empty");
            }

            if (contractVersion != ProtoConstants.ContractVersion)
            {
                throw new ValidationException($"Unsupported contract version: {contractVersion}");
            }

            ContractVersion = contractVersion;
            KeyId = keyId;
        }
    }

    /// <summary>
    /// Response class for describe-key command.
    /// </summary>
    public class DescribeKeyResponse
    {
        [JsonPropertyName("keyId")]
        public string KeyId { get; }

        [JsonPropertyName("keySpec")]
        public string KeySpec { get; }

        public DescribeKeyResponse(string keyId, string keySpec)
        {
            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentNullException(nameof(keyId), "KeyId must not be null or empty");
            }

            if (string.IsNullOrEmpty(keySpec))
            {
                throw new ArgumentNullException(nameof(keySpec), "KeySpec must not be null or empty");
            }

            KeyId = keyId;
            KeySpec = keySpec;
        }
    }
}