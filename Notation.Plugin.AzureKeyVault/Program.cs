using System.Text.Json;
using Notation.Plugin.AzureKeyVault.Command;
using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault
{
    public class Program
    {
        public static async Task Main(string[] args)
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
            catch (Azure.RequestFailedException e)
            {
                // wrap azure exception to notation plugin error response
                var rawResponse = e.GetRawResponse();
                if (rawResponse != null)
                {
                    var content = JsonDocument.Parse(rawResponse.Content);
                    if (content.RootElement.TryGetProperty("error", out var errorInfo) &&
                            errorInfo.TryGetProperty("message", out var errMsg))
                    {
                        var errorMessage = errMsg.GetString();
                        if (!string.IsNullOrEmpty(errorMessage))
                        {
                            Error.PrintError(
                                errorCode: e.ErrorCode ?? Error.ERROR,
                                errorMessage: errorMessage);
                            Environment.Exit(1);
                        }
                    }
                }

                // fallback to default error message
                Error.PrintError(Error.ERROR, e.Message);
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Error.PrintError(Error.ERROR, e.Message);
                Environment.Exit(1);
            }
        }

        public static async Task ExecuteAsync(string[] args)
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
  https://github.com/notaryproject/notaryproject/blob/v1.0.0/specs/plugin-extensibility.md#plugin-contract");
        }
    }
}
