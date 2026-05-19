namespace Notation.Plugin.Protocol
{
    /// <summary>
    /// KeySpec class include the type and size of the key.
    /// </summary>
    public static class KeySpecConstants
    {
        // RSASSA-PSS with SHA-256
        public const string RSA_2048 = "RSA-2048";
        // RSASSA-PSS with SHA-384
        public const string RSA_3072 = "RSA-3072";
        // RSASSA-PSS with SHA-512
        public const string RSA_4096 = "RSA-4096";
        // ECDSA on secp256r1 with SHA-256
        public const string EC_256 = "EC-256";
        // ECDSA on secp384r1 with SHA-384
        public const string EC_384 = "EC-384";
        // ECDSA on secp521r1 with SHA-512
        public const string EC_521 = "EC-521";
    }

    /// <summary>
    /// defines the SigningAlgorithm constants.
    /// </summary>
    public static class SigningAlgorithms
    {
        // RSASSA-PSS (default for JWS/COSE)
        public const string RSASSA_PSS_SHA_256 = "RSASSA-PSS-SHA-256";
        public const string RSASSA_PSS_SHA_384 = "RSASSA-PSS-SHA-384";
        public const string RSASSA_PSS_SHA_512 = "RSASSA-PSS-SHA-512";

        // RSASSA-PKCS1-v1_5 (required for PKCS#7/dm-verity)
        public const string RSASSA_PKCS1_V1_5_SHA_256 = "RSASSA-PKCS1-v1_5-SHA-256";
        public const string RSASSA_PKCS1_V1_5_SHA_384 = "RSASSA-PKCS1-v1_5-SHA-384";
        public const string RSASSA_PKCS1_V1_5_SHA_512 = "RSASSA-PKCS1-v1_5-SHA-512";

        // ECDSA
        public const string ECDSA_SHA_256 = "ECDSA-SHA-256";
        public const string ECDSA_SHA_384 = "ECDSA-SHA-384";
        public const string ECDSA_SHA_512 = "ECDSA-SHA-512";
    }

    /// <summary>
    /// Defines the supported signing schemes (padding modes for RSA).
    /// </summary>
    public static class SigningSchemes
    {
        /// <summary>
        /// RSASSA-PSS padding (default for JWS/COSE notation signatures).
        /// </summary>
        public const string RSASSA_PSS = "rsassa-pss";

        /// <summary>
        /// RSASSA-PKCS1-v1_5 padding (required for PKCS#7/dm-verity signatures).
        /// This scheme is needed for Linux kernel dm-verity verification which
        /// only supports PKCS#1 v1.5 signatures.
        /// </summary>
        public const string RSASSA_PKCS1_V1_5 = "rsassa-pkcs1-v1_5";
    }

    /// <summary>
    /// KeyType class.
    /// </summary>
    public enum KeyType
    {
        // EC is Elliptic Curve Cryptography
        EC,
        // RSA is Rivest–Shamir–Adleman Cryptography
        RSA
    }

    /// <summary>
    /// KeySpec class.
    /// </summary>
    public class KeySpec
    {
        public KeyType Type { get; }
        public int Size { get; }
        public KeySpec(KeyType type, int size)
        {
            Type = type;
            Size = size;
        }

        /// <summary>
        /// Encodes the key spec to be string.
        /// Supported key types are RSA with key size 2048, 3072, 4096
        /// and ECDSA with key size 256, 384, 521.
        ///
        /// <param name="keySpec">The key spec to be encoded</param>
        ///
        /// <returns>
        /// The encoded key spec, including RSA-2048, RSA-3072, RSA-4096, EC-256, EC-384, EC-521
        /// </returns>
        /// </summary>
        public string EncodeKeySpec() => Type switch
        {
            KeyType.RSA => Size switch
            {
                2048 => KeySpecConstants.RSA_2048,
                3072 => KeySpecConstants.RSA_3072,
                4096 => KeySpecConstants.RSA_4096,
                _ => throw new ArgumentException($"Invalid RSA KeySpec size {Size}")
            },
            KeyType.EC => Size switch
            {
                256 => KeySpecConstants.EC_256,
                384 => KeySpecConstants.EC_384,
                521 => KeySpecConstants.EC_521,
                _ => throw new ArgumentException($"Invalid EC KeySpec size {Size}")
            },
            _ => throw new ArgumentException($"Invalid KeySpec Type: {Type}")
        };

        /// <summary>
        /// Convert KeySpec to be SigningAlgorithm string (RSASSA-PSS for RSA, default).
        /// Supported key types are RSA with key size 2048, 3072, 4096
        /// and ECDSA with key size 256, 384, 521.
        /// </summary>
        public string ToSigningAlgorithm() => Type switch
        {
            KeyType.RSA => Size switch
            {
                2048 => SigningAlgorithms.RSASSA_PSS_SHA_256,
                3072 => SigningAlgorithms.RSASSA_PSS_SHA_384,
                4096 => SigningAlgorithms.RSASSA_PSS_SHA_512,
                _ => throw new ArgumentException($"Invalid RSA KeySpec size {Size}")
            },
            KeyType.EC => Size switch
            {
                256 => SigningAlgorithms.ECDSA_SHA_256,
                384 => SigningAlgorithms.ECDSA_SHA_384,
                521 => SigningAlgorithms.ECDSA_SHA_512,
                _ => throw new ArgumentException($"Invalid EC KeySpec size {Size}")
            },
            _ => throw new ArgumentException($"Invalid KeySpec Type: {Type}")
        };

        /// <summary>
        /// Get the SigningAlgorithm string for RSASSA-PKCS1-v1_5 signing.
        /// Uses RSASSA-PKCS1-v1_5-SHA-256/384/512 for RSA-2048/3072/4096 (required for
        /// PKCS#7/dm-verity). EC keys fall back to ECDSA-SHA-256/384/512 so the helper
        /// stays consistent with the default RSASSA-PSS overload.
        /// </summary>
        internal string ToSigningAlgorithmPKCS1() => Type switch
        {
            KeyType.RSA => Size switch
            {
                2048 => SigningAlgorithms.RSASSA_PKCS1_V1_5_SHA_256,
                3072 => SigningAlgorithms.RSASSA_PKCS1_V1_5_SHA_384,
                4096 => SigningAlgorithms.RSASSA_PKCS1_V1_5_SHA_512,
                _ => throw new ArgumentException($"Invalid RSA KeySpec size {Size}")
            },
            KeyType.EC => Size switch
            {
                // ECDSA doesn't have padding variants - use the same algorithm
                256 => SigningAlgorithms.ECDSA_SHA_256,
                384 => SigningAlgorithms.ECDSA_SHA_384,
                521 => SigningAlgorithms.ECDSA_SHA_512,
                _ => throw new ArgumentException($"Invalid EC KeySpec size {Size}")
            },
            _ => throw new ArgumentException($"Invalid KeySpec Type: {Type}")
        };

        /// <summary>
        /// Convert KeySpec to SigningAlgorithm string based on the specified signing scheme.
        /// </summary>
        /// <param name="scheme">The signing scheme to use (rsassa-pss or rsassa-pkcs1-v1_5)</param>
        /// <returns>The appropriate signing algorithm string</returns>
        // NOTE: keep in lock-step with KeySpecExtension.ToKeyVaultSignatureAlgorithm(string?).
        public string ToSigningAlgorithm(string? scheme) => scheme?.ToLowerInvariant() switch
        {
            SigningSchemes.RSASSA_PKCS1_V1_5 => ToSigningAlgorithmPKCS1(),
            SigningSchemes.RSASSA_PSS => ToSigningAlgorithm(),
            null or "" => ToSigningAlgorithm(),
            _ => throw new ArgumentException($"Invalid signing scheme: {scheme}. Supported values are '{SigningSchemes.RSASSA_PSS}' and '{SigningSchemes.RSASSA_PKCS1_V1_5}'")
        };
    }
}
