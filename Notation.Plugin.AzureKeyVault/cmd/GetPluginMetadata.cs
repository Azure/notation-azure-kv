using Notation.Plugin.Proto;

namespace Notation.Plugin.AzureKeyVault.Cmd
{
    /// <summary>
    /// Implementation of get-plugin-metadata command.
    /// </summary>
    public class GetPluginMetadata : IPluginCommand
    {
        public async Task<object> RunAsync(string _)
        {
            return await Task.FromResult<object>(new GetMetadataResponse(
                name: "azure-kv",
                description: "Notation Azure Key Vault plugin",
                version: "1.0.0",
                url: "https://github.com/Azure/notation-azure-kv",
                supportedContractVersions: new[] { ProtoConstants.ContractVersion },
                capabilities: new[] { "SIGNATURE_GENERATOR.RAW" }
            ));
        }
    }
}