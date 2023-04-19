using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Notation.Plugin.Proto;

namespace Notation.Plugin.AzureKeyVault.Cmd
{
    /// <summary>
    /// Implementation of describe-key command.
    /// </summary>
    public class GenerateSignature : IPluginCommand
    {
        public Task<object> RunAsync(string inputJson)
        {
            // parse the input
            GenerateSignatureRequest? input = JsonSerializer.Deserialize<GenerateSignatureRequest>(inputJson);
            if (input == null)
            {
                throw new ValidationException("Invalid input");
            }

            throw new NotImplementedException();
        }
    }
}