using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Notation.Plugin.Protocol
{
    /// <summary>
    /// KeySpec class include the type and size of the key.
    /// </summary>
    public static class KeySpecConstants
    {
        // RSASSA-PSS with SHA-256
        public const string RSA2048 = "RSA-2048";
        // RSASSA-PSS with SHA-384
        public const string RSA3072 = "RSA-3072";
        // RSASSA-PSS with SHA-512
        public const string RSA4096 = "RSA-4096";
        // ECDSA on secp256r1 with SHA-256
        public const string EC256 = "EC-256";
        // ECDSA on secp384r1 with SHA-384
        public const string EC384 = "EC-384";
        // ECDSA on secp521r1 with SHA-512
        public const string EC521 = "EC-521";
    }

    /// <summary>
    /// defines the SigningAlgorithm constants.
    /// </summary>
    public static class SigningAlgorithms
    {
        public const string RSASSA_PSS_SHA_256 = "RSASSA-PSS-SHA-256";
        public const string RSASSA_PSS_SHA_384 = "RSASSA-PSS-SHA-384";
        public const string RSASSA_PSS_SHA_512 = "RSASSA-PSS-SHA-512";
        public const string ECDSA_SHA_256 = "ECDSA-SHA-256";
        public const string ECDSA_SHA_384 = "ECDSA-SHA-384";
        public const string ECDSA_SHA_512 = "ECDSA-SHA-512";
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
    }

    /// <summary>
    /// KeySpecUtils class provides utility methods for Extracting KeySpec from 
    /// a certificate and for encode the keySpec to a string.
    /// </summary>
    public static class KeySpecUtils
    {
        /// <summary>
        /// Extracts the key spec from the certificate.
        /// Supported key types are RSA with key size 2048, 3072, 4096 
        /// and ECDSA with key size 256, 384, 521.
        /// 
        /// <param name="signingCert">The certificate to be extracted</param>
        ///
        /// <returns>The extracted key spec</returns>
        /// </summary>
        public static KeySpec ExtractKeySpec(X509Certificate2 signingCert)
        {
            RSA? rsaKey = signingCert.GetRSAPublicKey();
            if (rsaKey != null)
            {
                if (rsaKey.KeySize is 2048 or 3072 or 4096)
                {
                    return new KeySpec(KeyType.RSA, rsaKey.KeySize);
                }

                throw new ValidationException($"RSA key size {rsaKey.KeySize} bits is not supported");
            }

            ECDsa? ecdsaKey = signingCert.GetECDsaPublicKey();
            if (ecdsaKey != null)
            {
                if (ecdsaKey.KeySize is 256 or 384 or 521)
                {
                    return new KeySpec(KeyType.EC, ecdsaKey.KeySize);
                }

                throw new ValidationException($"ECDSA key size {ecdsaKey.KeySize} bits is not supported");
            }

            throw new ValidationException("Unsupported public key type");
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
        public static string EncodeKeySpec(KeySpec keySpec) => keySpec.Type switch
        {
            KeyType.RSA => keySpec.Size switch
            {
                2048 => KeySpecConstants.RSA2048,
                3072 => KeySpecConstants.RSA3072,
                4096 => KeySpecConstants.RSA4096,
                _ => throw new ArgumentException($"Invalid RSA KeySpec size {keySpec.Size}")
            },
            KeyType.EC => keySpec.Size switch
            {
                256 => KeySpecConstants.EC256,
                384 => KeySpecConstants.EC384,
                521 => KeySpecConstants.EC521,
                _ => throw new ArgumentException($"Invalid EC KeySpec size {keySpec.Size}")
            },
            _ => throw new ArgumentException($"Invalid KeySpec {keySpec}")
        };


        public static string ToSigningAlgorithm(KeySpec keySpec) => keySpec.Type switch
        {
            KeyType.RSA => keySpec.Size switch
            {
                2048 => SigningAlgorithms.RSASSA_PSS_SHA_256,
                3072 => SigningAlgorithms.RSASSA_PSS_SHA_384,
                4096 => SigningAlgorithms.RSASSA_PSS_SHA_512,
                _ => throw new ArgumentException($"Invalid RSA KeySpec size {keySpec.Size}")
            },
            KeyType.EC => keySpec.Size switch
            {
                256 => SigningAlgorithms.ECDSA_SHA_256,
                384 => SigningAlgorithms.ECDSA_SHA_384,
                521 => SigningAlgorithms.ECDSA_SHA_512,
                _ => throw new ArgumentException($"Invalid EC KeySpec size {keySpec.Size}")
            },
            _ => throw new ArgumentException($"Invalid KeySpec {keySpec}")
        };
    }
}