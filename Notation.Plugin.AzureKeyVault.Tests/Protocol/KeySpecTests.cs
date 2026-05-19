using System;
using Xunit;

namespace Notation.Plugin.Protocol.Tests
{
    public class KeySpecTests
    {
        [Theory]
        [InlineData(KeyType.RSA, 2048, "RSA-2048", "RSASSA-PSS-SHA-256")]
        [InlineData(KeyType.RSA, 3072, "RSA-3072", "RSASSA-PSS-SHA-384")]
        [InlineData(KeyType.RSA, 4096, "RSA-4096", "RSASSA-PSS-SHA-512")]
        [InlineData(KeyType.EC, 256, "EC-256", "ECDSA-SHA-256")]
        [InlineData(KeyType.EC, 384, "EC-384", "ECDSA-SHA-384")]
        [InlineData(KeyType.EC, 521, "EC-521", "ECDSA-SHA-512")]
        public void KeySpec_EncodeKeySpecAndToSigningAlgorithm_ReturnsCorrectValues(KeyType keyType, int size, string expectedKeySpec, string expectedSigningAlgorithm)
        {
            // Arrange
            KeySpec keySpec = new KeySpec(keyType, size);

            // Act
            string encodedKeySpec = keySpec.EncodeKeySpec();
            string signingAlgorithm = keySpec.ToSigningAlgorithm();

            // Assert
            Assert.Equal(expectedKeySpec, encodedKeySpec);
            Assert.Equal(expectedSigningAlgorithm, signingAlgorithm);
        }

        [Theory]
        [InlineData(KeyType.RSA, 1024)]
        [InlineData(KeyType.RSA, 3070)]
        [InlineData(KeyType.EC, 128)]
        [InlineData(KeyType.EC, 500)]
        public void KeySpec_EncodeKeySpecAndToSigningAlgorithm_ThrowsArgumentExceptionForInvalidSizes(KeyType keyType, int size)
        {
            // Arrange
            KeySpec keySpec = new KeySpec(keyType, size);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => keySpec.EncodeKeySpec());
            Assert.Throws<ArgumentException>(() => keySpec.ToSigningAlgorithm());
        }

        [Theory]
        [InlineData(KeyType.RSA, 2048, "RSASSA-PKCS1-v1_5-SHA-256")]
        [InlineData(KeyType.RSA, 3072, "RSASSA-PKCS1-v1_5-SHA-384")]
        [InlineData(KeyType.RSA, 4096, "RSASSA-PKCS1-v1_5-SHA-512")]
        [InlineData(KeyType.EC, 256, "ECDSA-SHA-256")]
        [InlineData(KeyType.EC, 384, "ECDSA-SHA-384")]
        [InlineData(KeyType.EC, 521, "ECDSA-SHA-512")]
        public void KeySpec_ToSigningAlgorithmPKCS1_ReturnsCorrectValues(KeyType keyType, int size, string expectedSigningAlgorithm)
        {
            // Arrange
            KeySpec keySpec = new KeySpec(keyType, size);

            // Act
            string signingAlgorithm = keySpec.ToSigningAlgorithmPKCS1();

            // Assert
            Assert.Equal(expectedSigningAlgorithm, signingAlgorithm);
        }

        [Theory]
        [InlineData(KeyType.RSA, 1024)]
        [InlineData(KeyType.EC, 128)]
        public void KeySpec_ToSigningAlgorithmPKCS1_ThrowsArgumentExceptionForInvalidSizes(KeyType keyType, int size)
        {
            // Arrange
            KeySpec keySpec = new KeySpec(keyType, size);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => keySpec.ToSigningAlgorithmPKCS1());
        }

        [Theory]
        [InlineData(KeyType.RSA, 2048, null, "RSASSA-PSS-SHA-256")]                          // Default: PSS
        [InlineData(KeyType.RSA, 2048, "", "RSASSA-PSS-SHA-256")]                             // Empty: PSS
        [InlineData(KeyType.RSA, 2048, "rsassa-pss", "RSASSA-PSS-SHA-256")]                   // Explicit PSS
        [InlineData(KeyType.RSA, 2048, "rsassa-pkcs1-v1_5", "RSASSA-PKCS1-v1_5-SHA-256")]    // PKCS1 routing
        [InlineData(KeyType.RSA, 3072, "rsassa-pkcs1-v1_5", "RSASSA-PKCS1-v1_5-SHA-384")]    // PKCS1 3072
        [InlineData(KeyType.RSA, 4096, "rsassa-pkcs1-v1_5", "RSASSA-PKCS1-v1_5-SHA-512")]    // PKCS1 4096
        [InlineData(KeyType.EC, 256, "rsassa-pkcs1-v1_5", "ECDSA-SHA-256")]                   // EC unaffected
        [InlineData(KeyType.EC, 384, "rsassa-pkcs1-v1_5", "ECDSA-SHA-384")]                   // EC 384
        [InlineData(KeyType.EC, 521, "rsassa-pkcs1-v1_5", "ECDSA-SHA-512")]                   // EC 521
        public void KeySpec_ToSigningAlgorithmWithScheme_ReturnsCorrectValues(KeyType keyType, int size, string? scheme, string expectedSigningAlgorithm)
        {
            // Arrange
            KeySpec keySpec = new KeySpec(keyType, size);

            // Act
            string signingAlgorithm = keySpec.ToSigningAlgorithm(scheme);

            // Assert
            Assert.Equal(expectedSigningAlgorithm, signingAlgorithm);
        }

        [Fact]
        public void KeySpec_ToSigningAlgorithmWithInvalidScheme_ThrowsArgumentException()
        {
            // Arrange
            KeySpec keySpec = new KeySpec(KeyType.RSA, 2048);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => keySpec.ToSigningAlgorithm("invalid-scheme"));
            Assert.Contains("invalid-scheme", ex.Message);
            Assert.Contains("rsassa-pss", ex.Message);
            Assert.Contains("rsassa-pkcs1-v1_5", ex.Message);
        }
    }
}
