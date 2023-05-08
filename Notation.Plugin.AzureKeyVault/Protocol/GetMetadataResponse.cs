using System.Text.Json;
using System.Text.Json.Serialization;

namespace Notation.Plugin.Protocol
{
    /// <summary>
    /// Response class for get-plugin-metadata command which returns the information about the plugin.
    /// This class implements the <a href="https://github.com/notaryproject/notaryproject/blob/main/specs/plugin-extensibility.md#plugin-metadata">get-plugin-metadata</a> response.
    /// </summary>
    public class GetMetadataResponse : IPluginResponse
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
            string[] capabilities) : base()
        {
            Name = name;
            Description = description;
            Version = version;
            Url = url;
            SupportedContractVersions = supportedContractVersions;
            Capabilities = capabilities;
        }

        /// <summary>
        /// Serializes the response object to JSON string.
        /// </summary>
        public string ToJson()
        {
            return JsonSerializer.Serialize(
                value: this,
                jsonTypeInfo: new GetMetadataResponseContext(PluginIO.GetRelaxedJsonSerializerOptions()).GetMetadataResponse);
        }
    }

    /// <summary>
    /// The context class for serializing/deserializing.
    /// </summary>
    [JsonSerializable(typeof(GetMetadataResponse))]
    internal partial class GetMetadataResponseContext : JsonSerializerContext { }
}
