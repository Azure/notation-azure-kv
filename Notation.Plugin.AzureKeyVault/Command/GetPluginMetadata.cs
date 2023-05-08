using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault.Command
{
    /// <summary>
    /// Implementation of get-plugin-metadata command.
    /// </summary>
    public partial class GetPluginMetadata : IPluginCommand
    {
        public static readonly string Version;
        public static readonly string CommitHash;

        public async Task<IPluginResponse> RunAsync()
        {
            return await Task.FromResult<IPluginResponse>(new GetMetadataResponse(
                name: "azure-kv",
                description: "Notation Azure Key Vault plugin",
                version: Version,
                url: "https://github.com/Azure/notation-azure-kv",
                supportedContractVersions: new[] { Protocol.Protocol.ContractVersion },
                capabilities: new[] { "SIGNATURE_GENERATOR.RAW" }
            ));
        }
    }
}