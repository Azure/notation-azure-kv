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

        [Theory]
        [InlineData(KeyType.RSA, 2048, "RS256")]
        [InlineData(KeyType.RSA, 3072, "RS384")]
        [InlineData(KeyType.RSA, 4096, "RS512")]
        [InlineData(KeyType.EC, 256, "ES256")]
        [InlineData(KeyType.EC, 384, "ES384")]
        [InlineData(KeyType.EC, 521, "ES512")]
        public void ToKeyVaultSignatureAlgorithmPKCS1_ValidKeySpecs_ReturnsCorrectAlgorithm(KeyType keyType, int keySize, string expectedAlgorithm)
        {
            // Arrange
            var keySpec = new KeySpec(keyType, keySize);

            // Act
            var signatureAlgorithm = keySpec.ToKeyVaultSignatureAlgorithmPKCS1();

            // Assert
            Assert.Equal(expectedAlgorithm, signatureAlgorithm);
        }

        [Theory]
        [InlineData(KeyType.RSA, 1024)]
        [InlineData(KeyType.EC, 128)]
        public void ToKeyVaultSignatureAlgorithmPKCS1_InvalidKeySpecs_ThrowsArgumentException(KeyType keyType, int keySize)
        {
            // Arrange
            var keySpec = new KeySpec(keyType, keySize);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => keySpec.ToKeyVaultSignatureAlgorithmPKCS1());
        }

        [Theory]
        [InlineData(KeyType.RSA, 2048, null, "PS256")]                         // Default: PSS
        [InlineData(KeyType.RSA, 2048, "", "PS256")]                           // Empty: PSS
        [InlineData(KeyType.RSA, 2048, "rsassa-pss", "PS256")]                 // Explicit PSS
        [InlineData(KeyType.RSA, 2048, "rsassa-pkcs1-v1_5", "RS256")]         // PKCS1 routing
        [InlineData(KeyType.RSA, 3072, "rsassa-pkcs1-v1_5", "RS384")]         // PKCS1 3072
        [InlineData(KeyType.RSA, 4096, "rsassa-pkcs1-v1_5", "RS512")]         // PKCS1 4096
        [InlineData(KeyType.EC, 256, "rsassa-pkcs1-v1_5", "ES256")]           // EC unaffected
        [InlineData(KeyType.EC, 384, "rsassa-pkcs1-v1_5", "ES384")]           // EC 384
        [InlineData(KeyType.EC, 521, "rsassa-pkcs1-v1_5", "ES512")]           // EC 521
        public void ToKeyVaultSignatureAlgorithm_WithScheme_ReturnsCorrectAlgorithm(KeyType keyType, int keySize, string? scheme, string expectedAlgorithm)
        {
            // Arrange
            var keySpec = new KeySpec(keyType, keySize);

            // Act
            var signatureAlgorithm = keySpec.ToKeyVaultSignatureAlgorithm(scheme);

            // Assert
            Assert.Equal(expectedAlgorithm, signatureAlgorithm);
        }

        [Fact]
        public void ToKeyVaultSignatureAlgorithm_WithInvalidScheme_ThrowsArgumentException()
        {
            // Arrange
            var keySpec = new KeySpec(KeyType.RSA, 2048);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => keySpec.ToKeyVaultSignatureAlgorithm("invalid-scheme"));
            Assert.Contains("invalid-scheme", ex.Message);
            Assert.Contains("rsassa-pss", ex.Message);
            Assert.Contains("rsassa-pkcs1-v1_5", ex.Message);
        }
    }
}
