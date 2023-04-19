using System.Text.Json.Serialization;

namespace Notation.Plugin.Proto
{
    /// <summary>
    /// Request class for generate-signature command.
    /// </summary>
    public class GenerateSignatureRequest
    {
        [JsonPropertyName("contractVersion")]
        public string ContractVersion { get; }

        [JsonPropertyName("keyId")]
        public string KeyId { get; }

        [JsonPropertyName("pluginConfig")]
        public Dictionary<string, string>? PluginConfig { get; }

        [JsonPropertyName("keySpec")]
        public string KeySpec { get; }

        [JsonPropertyName("hashAlgorithm")]
        public string HashAlgorithm { get; }

        [JsonPropertyName("payload")]
        public string Payload { get; }

        public GenerateSignatureRequest(string contractVersion, string keyId, Dictionary<string, string>? pluginConfig, string keySpec, string hashAlgorithm, string payload)
        {
            if (string.IsNullOrEmpty(contractVersion))
            {
                throw new ArgumentNullException(nameof(contractVersion), "ContractVersion must not be null or empty");
            }

            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentNullException(nameof(keyId), "KeyId must not be null or empty");
            }

            if (string.IsNullOrEmpty(keySpec))
            {
                throw new ArgumentNullException(nameof(keySpec), "KeySpec must not be null or empty");
            }

            if (string.IsNullOrEmpty(hashAlgorithm))
            {
                throw new ArgumentNullException(nameof(hashAlgorithm), "HashAlgorithm must not be null or empty");
            }

            if (string.IsNullOrEmpty(payload))
            {
                throw new ArgumentNullException(nameof(payload), "Payload must not be null or empty");
            }

            if (contractVersion != "1.0")
            {
                throw new ValidationException($"Unsupported contract version: {contractVersion}");
            }

            ContractVersion = contractVersion;
            KeyId = keyId;
            PluginConfig = pluginConfig;
            KeySpec = keySpec;
            HashAlgorithm = hashAlgorithm;
            Payload = payload;
        }
    }

    /// <summary>
    /// Response class for generate-signature command.
    /// </summary>
    public class GenerateSignatureResponse
    {
        [JsonPropertyName("keyId")]
        public string KeyId { get; }

        [JsonPropertyName("signature")]
        public string Signature { get; }

        [JsonPropertyName("signingAlgorithm")]
        public string SigningAlgorithm { get; }

        [JsonPropertyName("certificateChain")]
        public List<string> CertificateChain { get; }

        public GenerateSignatureResponse(string keyId, string signature, string signingAlgorithm, List<string> certificateChain)
        {
            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentNullException(nameof(keyId), "KeyId must not be null or empty");
            }

            if (string.IsNullOrEmpty(signature))
            {
                throw new ArgumentNullException(nameof(signature), "Signature must not be null or empty");
            }

            if (string.IsNullOrEmpty(signingAlgorithm))
            {
                throw new ArgumentNullException(nameof(signingAlgorithm), "SigningAlgorithm must not be null or empty");
            }

            if (certificateChain == null)
            {
                throw new ArgumentNullException(nameof(certificateChain), "CertificateChain must not be null");
            }

            KeyId = keyId;
            Signature = signature;
            SigningAlgorithm = signingAlgorithm;
            CertificateChain = certificateChain;
        }
    }
}
