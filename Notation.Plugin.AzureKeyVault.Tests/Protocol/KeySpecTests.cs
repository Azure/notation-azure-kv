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
    }
}
