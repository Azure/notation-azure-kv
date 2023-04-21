using System.Security.Cryptography;  

namespace Notation.Plugin.Protocol
{
    public static class HashAlgorithmHelper
    {
        public static HashAlgorithm FromKeySpec(KeySpec keySpec) => keySpec.Type switch
        {
            KeyType.EC => keySpec.Size switch
            {
                256 => SHA256.Create(),
                384 => SHA384.Create(),
                521 => SHA512.Create(),
                _ => throw new ArgumentException($"Invalid KeySpec for EC with size {keySpec.Size}")
            },
            KeyType.RSA => keySpec.Size switch
            {
                2048 => SHA256.Create(),
                3072 => SHA384.Create(),
                4096 => SHA512.Create(),
                _ => throw new ArgumentException($"Invalid KeySpec for RSA with size {keySpec.Size}")
            },
            _ => throw new ArgumentException($"Invalid KeySpec with type {keySpec.Type}")
        };
    }

    
}