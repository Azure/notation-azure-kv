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
        public byte[] Payload { get; }

        public GenerateSignatureRequest(string contractVersion, string keyId, Dictionary<string, string>? pluginConfig, string keySpec, string hashAlgorithm, byte[] payload)
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

            if (payload == null || payload.Length == 0)
            {
                throw new ArgumentNullException(nameof(payload), "Payload must not be null or empty");
            }

            if (contractVersion != Protocol.ContractVersion)
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
    /// This class implements the <a href="https://github.com/notaryproject/notaryproject/blob/main/specs/plugin-extensibility.md#generate-signature">generate-signature</a> response.
    /// </summary>
    public class GenerateSignatureResponse
    {
        [JsonPropertyName("keyId")]
        public string KeyId { get; }

        [JsonPropertyName("signature")]
        public byte[] Signature { get; }

        [JsonPropertyName("signingAlgorithm")]
        public string SigningAlgorithm { get; }

        [JsonPropertyName("certificateChain")]
        public List<byte[]> CertificateChain { get; }

        public GenerateSignatureResponse(string keyId, byte[] signature, string signingAlgorithm, List<byte[]> certificateChain)
        {
            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentNullException(nameof(keyId), "KeyId must not be null or empty");
            }

            if (signature == null || signature.Length == 0)
            {
                throw new ArgumentNullException(nameof(signature), "Signature must not be null or empty");
            }

            if (string.IsNullOrEmpty(signingAlgorithm))
            {
                throw new ArgumentNullException(nameof(signingAlgorithm), "SigningAlgorithm must not be null or empty");
            }

            if (certificateChain == null || certificateChain.Count == 0)
            {
                throw new ArgumentNullException(nameof(certificateChain), "CertificateChain must not be null or empty");
            }

            KeyId = keyId;
            Signature = signature;
            SigningAlgorithm = signingAlgorithm;
            CertificateChain = certificateChain;
        }
    }
}
