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
        /// Uses RSASSA-PSS (PS256/PS384/PS512) for RSA keys (default for JWS/COSE).
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

        /// <summary>
        /// Get SignatureAlgorithm from KeySpec for Azure Key Vault signing using RSASSA-PKCS1-v1_5.
        /// Uses RS256/RS384/RS512 for RSA keys (required for PKCS#7/dm-verity).
        /// For EC keys, returns the standard ECDSA algorithm (no padding change).
        /// </summary>
        public static SignatureAlgorithm ToKeyVaultSignatureAlgorithmPKCS1(this KeySpec keySpec) => keySpec.Type switch
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
                // ECDSA doesn't have padding variants - use the same algorithm
                256 => SignatureAlgorithm.ES256,
                384 => SignatureAlgorithm.ES384,
                521 => SignatureAlgorithm.ES512,
                _ => throw new ArgumentException($"Invalid KeySpec for EC with size {keySpec.Size}")
            },
            _ => throw new ArgumentException($"Invalid KeySpec with type {keySpec.Type}")
        };

        /// <summary>
        /// Get SignatureAlgorithm from KeySpec for Azure Key Vault signing based on the specified signing scheme.
        /// </summary>
        /// <param name="keySpec">The key specification</param>
        /// <param name="scheme">The signing scheme (rsassa-pss or rsassa-pkcs1-v1_5). Default is rsassa-pss.</param>
        /// <returns>The Azure Key Vault SignatureAlgorithm</returns>
        public static SignatureAlgorithm ToKeyVaultSignatureAlgorithm(this KeySpec keySpec, string? scheme) => scheme?.ToLowerInvariant() switch
        {
            SigningScheme.RSASSA_PKCS1_V1_5 => keySpec.ToKeyVaultSignatureAlgorithmPKCS1(),
            SigningScheme.RSASSA_PSS => keySpec.ToKeyVaultSignatureAlgorithm(),
            null => keySpec.ToKeyVaultSignatureAlgorithm(),
            "" => keySpec.ToKeyVaultSignatureAlgorithm(),
            _ => throw new ArgumentException($"Invalid signing scheme: {scheme}. Supported values are '{SigningScheme.RSASSA_PSS}' and '{SigningScheme.RSASSA_PKCS1_V1_5}'")
        };
    }
}
