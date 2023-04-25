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
                throw new ValidationException("Missing command");
            }

            // read the input
            var inputJson = PluginIO.ReadInput();

            IPluginCommand? cmd = null;
            switch (args[0])
            {
                case "get-plugin-metadata":
                    cmd = new GetPluginMetadata();
                    break;
                case "describe-key":
                    cmd = new DescribeKey(inputJson);
                    break;
                case "generate-signature":
                    cmd = new GenerateSignature(inputJson);
                    break;
                default:
                    throw new ValidationException($"Invalid command: {args[0]}");
            }

            // execute the command
            var resp = await cmd.RunAsync();

            // print the output
            PluginIO.WriteOutput(resp);
        }
    }
}
