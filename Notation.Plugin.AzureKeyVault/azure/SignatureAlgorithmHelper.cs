using Notation.Plugin.Proto;
using KeyType = Notation.Plugin.Proto.KeyType;
using Azure.Security.KeyVault.Keys.Cryptography;

namespace Notation.Plugin.AzureKeyVault
{
    class SignatureAlgorithmHelper
    {
        public static SignatureAlgorithm FromKeySpec(KeySpec keySpec) => keySpec.Type switch
        {
            KeyType.RSA => keySpec.Size switch
            {
                2048 => SignatureAlgorithm.RS256,
                3072 => SignatureAlgorithm.RS384,
                4096 => SignatureAlgorithm.RS512,
                _ => throw new ArgumentException($"Invalid KeySpec for RSA with size {keySpec.Size}")
            },
            KeyType.EC => keySpec.Size switch
            {
                256 => SignatureAlgorithm.ES256,
                384 => SignatureAlgorithm.ES384,
                521 => SignatureAlgorithm.ES512,
                _ => throw new ArgumentException($"Invalid KeySpec for EC with size {keySpec.Size}")
            },
            _ => throw new ArgumentException($"Invalid KeySpec with type {keySpec.Type}")
        };
    }
}