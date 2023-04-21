using System.Text.Json.Serialization;

namespace Notation.Plugin.Protocol
{
    public static class ProtoConstants
    {
        public const string ContractVersion = "1.0";
    }
    /// <summary>
    /// Response class for get-plugin-metadata command which returns the information about the plugin.
    /// This class implements the <a href="https://github.com/notaryproject/notaryproject/blob/main/specs/plugin-extensibility.md#plugin-metadata">get-plugin-metadata</a> response.
    /// </summary>
    public class GetMetadataResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("supportedContractVersions")]
        public string[] SupportedContractVersions { get; set; }

        [JsonPropertyName("capabilities")]
        public string[] Capabilities { get; set; }

        public GetMetadataResponse(
            string name,
            string description,
            string version,
            string url,
            string[] supportedContractVersions,
            string[] capabilities)
        {
            Name = name;
            Description = description;
            Version = version;
            Url = url;
            SupportedContractVersions = supportedContractVersions;
            Capabilities = capabilities;
        }
    }
}
