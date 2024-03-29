using System.Text.Json;
using System.Text.Json.Serialization;

namespace Notation.Plugin.Protocol
{
    /// <summary>
    /// Request class for describe-key command which is used to get metadata for a given key.
    /// The class implement the <a href="https://github.com/notaryproject/notaryproject/blob/main/specs/plugin-extensibility.md#describe-key">describe-key</a> request.
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

            if (contractVersion != Protocol.ContractVersion)
            {
                throw new ValidationException($"Unsupported contract version: {contractVersion}");
            }

            ContractVersion = contractVersion;
            KeyId = keyId;
        }
    }

    /// <summary>
    /// Context class for describe-key command.
    /// </summary>
    [JsonSerializable(typeof(DescribeKeyRequest))]
    internal partial class DescribeKeyRequestContext : JsonSerializerContext { }

    /// <summary>
    /// Response class for describe-key command.
    /// The class implement the <a href="https://github.com/notaryproject/notaryproject/blob/main/specs/plugin-extensibility.md#describe-key">describe-key</a> response.
    /// </summary>
    public class DescribeKeyResponse : IPluginResponse
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

        /// <summary>
        /// Serializes the response object to JSON string.
        /// </summary>
        public string ToJson()
        {
            return JsonSerializer.Serialize(
                value: this,
                jsonTypeInfo: new DescribeKeyResponseContext(PluginIO.GetRelaxedJsonSerializerOptions()).DescribeKeyResponse);
        }
    }

    /// <summary>
    /// Context class for describe-key command.
    /// </summary>
    [JsonSerializable(typeof(DescribeKeyResponse))]
    internal partial class DescribeKeyResponseContext : JsonSerializerContext { }
}
