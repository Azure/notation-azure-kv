using System;
using Notation.Plugin.Protocol;
using Xunit;

namespace Notation.Plugin.AzureKeyVault.Client.Tests
{
    public class KeySpecExtensionTests
    {
        [Theory]
        [InlineData(KeyType.RSA, 2048, "PS256")]
        [InlineData(KeyType.RSA, 3072, "PS384")]
        [InlineData(KeyType.RSA, 4096, "PS512")]
        [InlineData(KeyType.EC, 256, "ES256")]
        [InlineData(KeyType.EC, 384, "ES384")]
        [InlineData(KeyType.EC, 521, "ES512")]
        public void ToSignatureAlgorithm_ValidKeySpecs_ReturnsCorrectSignatureAlgorithm(KeyType keyType, int keySize, string expectedAlgorithm)
        {
            // Arrange
            var keySpec = new KeySpec(keyType, keySize);
            // Act
            var signatureAlgorithm = keySpec.ToKeyVaultSignatureAlgorithm();

            // Assert
            Assert.Equal(expectedAlgorithm, signatureAlgorithm);
        }

        [Theory]
        [InlineData(KeyType.RSA, 1024)]
        [InlineData(KeyType.EC, 128)]
        public void ToSignatureAlgorithm_InvalidKeySpecs_ThrowsArgumentException(KeyType keyType, int keySize)
        {
            // Arrange
            var keySpec = new KeySpec(keyType, keySize);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => keySpec.ToKeyVaultSignatureAlgorithm());
        }
    }
}