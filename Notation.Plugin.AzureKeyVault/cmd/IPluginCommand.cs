namespace Notation.Plugin.AzureKeyVault.Cmd
{
    /// <summary>
    /// Interface for plugin commands.
    /// </summary>
    public interface IPluginCommand{
        Task<object> RunAsync(string inputJson);
    }
}