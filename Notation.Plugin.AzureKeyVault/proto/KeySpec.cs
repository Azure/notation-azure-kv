using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Notation.Plugin.Proto
{
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
    /// KeySpecUtils class.
    /// </summary>
    public static class KeySpecUtils
    {
        public const string RSA2048 = "RSA-2048";
        public const string RSA3072 = "RSA-3072";
        public const string RSA4096 = "RSA-4096";
        public const string EC256 = "EC-256";
        public const string EC384 = "EC-384";
        public const string EC521 = "EC-521";

        /// <summary>
        /// Extracts the key spec from the certificate.
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
        /// </summary>
        public static string EncodeKeySpec(KeySpec keySpec)
        {
            switch (keySpec.Type)
            {
                case KeyType.RSA:
                    switch (keySpec.Size)
                    {
                        case 2048:
                            return RSA2048;
                        case 3072:
                            return RSA3072;
                        case 4096:
                            return RSA4096;
                        default:
                            throw new ArgumentException($"Invalid KeySpec {keySpec}");
                    }
                case KeyType.EC:
                    switch (keySpec.Size)
                    {
                        case 256:
                            return EC256;
                        case 384:
                            return EC384;
                        case 521:
                            return EC521;
                        default:
                            throw new ArgumentException($"Invalid KeySpec {keySpec}");
                    }
                default:
                    throw new ArgumentException($"Invalid KeySpec {keySpec}");
            }
        }
    }
}