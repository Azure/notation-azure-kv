using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Notation.Plugin.Protocol
{
    public static class CertificateExtension
    {
        /// <summary>
        /// Extracts the key spec from the certificate.
        /// Supported key types are RSA with key size 2048, 3072, 4096 
        /// and ECDSA with key size 256, 384, 521.
        /// 
        /// <returns>The extracted key spec</returns>
        /// </summary>
        public static KeySpec KeySpec(this X509Certificate2 certificate)
        {
            RSA? rsaKey = certificate.GetRSAPublicKey();
            if (rsaKey != null)
            {
                if (rsaKey.KeySize is 2048 or 3072 or 4096)
                {
                    return new KeySpec(KeyType.RSA, rsaKey.KeySize);
                }

                throw new ValidationException($"RSA key size {rsaKey.KeySize} bits is not supported");
            }

            ECDsa? ecdsaKey = certificate.GetECDsaPublicKey();
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
    }
}