using System.Text.Json.Serialization;

namespace Notation.Plugin.Protocol
{
    /// <summary>
    /// Request class for generate-signature command which is used to generate the raw signature for a given payload.
    /// This class implements the <a href="https://github.com/notaryproject/notaryproject/blob/main/specs/plugin-extensibility.md#generate-signature">generate-signature</a> request.
    /// </summary>
    public class GenerateSignatureRequest
    {
        [JsonPropertyName("contractVersion")]
        public string? ContractVersion { get; set; }

        [JsonPropertyName("keyId")]
        public string? KeyId { get; set; }

        [JsonPropertyName("pluginConfig")]
        public Dictionary<string, string>? PluginConfig { get; set; }

        [JsonPropertyName("keySpec")]
        public string? KeySpec { get; set; }

        [JsonPropertyName("hashAlgorithm")]
        public string? HashAlgorithm { get; set; }

        [JsonPropertyName("payload")]
        public string? Payload { get; set; }
    }

    /// <summary>
    /// Response class for generate-signature command.
    /// This class implements the <a href="https://github.com/notaryproject/notaryproject/blob/main/specs/plugin-extensibility.md#generate-signature">generate-signature</a> response.
    /// </summary>
    public class GenerateSignatureResponse
    {
        [JsonPropertyName("keyId")]
        public string? KeyId { get; set; }

        [JsonPropertyName("signature")]
        public string? Signature { get; set; }

        [JsonPropertyName("signingAlgorithm")]
        public string? SigningAlgorithm { get; set; }

        [JsonPropertyName("certificateChain")]
        public List<string>? CertificateChain { get; set; }
    }
}