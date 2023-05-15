using Notation.Plugin.AzureKeyVault.Command;
using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                await ExecuteAsync(args);
            }
            catch (PluginException e)
            {
                Error.PrintError(e.Code, e.Message);
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Error.PrintError(Error.ERROR, e.Message);
                Environment.Exit(1);
            }
        }

        static async Task ExecuteAsync(string[] args)
        {
            if (args.Length < 1)
            {
                PrintHelp();
                return;
            }

            IPluginCommand? cmd = null;
            switch (args[0])
            {
                case "get-plugin-metadata":
                    cmd = new GetPluginMetadata();
                    break;
                case "describe-key":
                    cmd = new DescribeKey(PluginIO.ReadInput());
                    break;
                case "generate-signature":
                    cmd = new GenerateSignature(PluginIO.ReadInput());
                    break;
                default:
                    throw new ValidationException($"Invalid command: {args[0]}");
            }

            // execute the command
            var response = await cmd.RunAsync();

            // write output
            Console.WriteLine(response.ToJson());
        }

        static void PrintHelp()
        {
            Console.WriteLine(@$"notation-azure-kv - Notation - Azure Key Vault plugin

Usage:
  notation-azure-kv <command>

Version:
  {GetPluginMetadata.Version} 

Commit Hash:
  {GetPluginMetadata.CommitHash}

Commands:
  describe-key         Azure key description
  generate-signature   Sign artifacts with keys in Azure Key Vault
  get-plugin-metadata  Get plugin metadata

Documentation:
  https://github.com/notaryproject/notaryproject/blob/v1.0.0-rc.2/specs/plugin-extensibility.md#plugin-contract");
        }
    }
}
