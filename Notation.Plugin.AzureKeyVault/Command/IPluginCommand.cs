namespace Notation.Plugin.AzureKeyVault.Command
{
    /// <summary>
    /// Interface for plugin commands.
    /// </summary>
    public interface IPluginCommand
    {
        Task<object> RunAsync();
    }
}