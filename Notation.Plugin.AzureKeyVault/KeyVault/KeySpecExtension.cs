using Azure.Security.KeyVault.Keys.Cryptography;
using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault.Client
{
    /// <summary>
    /// Extension class to get SignatureAlgorithm from KeySpec.
    /// </summary>
    public static class KeySpecExtension
    {

        /// <summary>
        /// Get SignatureAlgorithm from KeySpec for Azure Key Vault signing.
        /// </summary>
        public static SignatureAlgorithm ToKeyVaultSignatureAlgorithm(this KeySpec keySpec) => keySpec.Type switch
        {
            KeyType.RSA => keySpec.Size switch
            {
                2048 => SignatureAlgorithm.PS256,
                3072 => SignatureAlgorithm.PS384,
                4096 => SignatureAlgorithm.PS512,
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