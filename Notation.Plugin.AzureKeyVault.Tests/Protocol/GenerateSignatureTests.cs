using System;
using System.Collections.Generic;
using Xunit;

namespace Notation.Plugin.Protocol.Tests
{
    public class GenerateSignatureTests
    {
        [Fact]
        public void GenerateSignatureRequest_ThrowsArgumentNullException_WhenFieldsAreEmptyOrNull()
        {
            // Arrange
            string contractVersion = "test-version";
            string keyId = "test-key-id";
            Dictionary<string, string> pluginConfig = new Dictionary<string, string> { { "key1", "value1" } };
            string keySpec = "test-key-spec";
            string hashAlgorithm = "SHA256";
            byte[] payload = new byte[] { 1, 2, 3, 4, 5 };

            // Assert
            Assert.Throws<ArgumentNullException>(() => new GenerateSignatureRequest(string.Empty, keyId, pluginConfig, keySpec, hashAlgorithm, payload));
            Assert.Throws<ArgumentNullException>(() => new GenerateSignatureRequest(contractVersion, string.Empty, pluginConfig, keySpec, hashAlgorithm, payload));
            Assert.Throws<ArgumentNullException>(() => new GenerateSignatureRequest(contractVersion, keyId, pluginConfig, string.Empty, hashAlgorithm, payload));
            Assert.Throws<ArgumentNullException>(() => new GenerateSignatureRequest(contractVersion, keyId, pluginConfig, keySpec, string.Empty, payload));
            Assert.Throws<ArgumentNullException>(() => new GenerateSignatureRequest(contractVersion, keyId, pluginConfig, keySpec, hashAlgorithm, new byte[0]));
        }

        [Fact]
        public void GenerateSignatureResponse_ThrowsArgumentNullException_WhenFieldsAreEmptyOrNull()
        {
            // Arrange
            string keyId = "test-key-id";
            byte[] signature = new byte[] { 1, 2, 3, 4, 5 };
            string signingAlgorithm = "RSA-PSS";
            List<byte[]> certificateChain = new List<byte[]> { new byte[] { 6, 7, 8, 9, 10 } };

            // Assert
            Assert.Throws<ArgumentNullException>(() => new GenerateSignatureResponse(string.Empty, signature, signingAlgorithm, certificateChain));
            Assert.Throws<ArgumentNullException>(() => new GenerateSignatureResponse(keyId, new byte[0], signingAlgorithm, certificateChain));
            Assert.Throws<ArgumentNullException>(() => new GenerateSignatureResponse(keyId, signature, string.Empty, certificateChain));
            Assert.Throws<ArgumentNullException>(() => new GenerateSignatureResponse(keyId, signature, signingAlgorithm, new List<byte[]>()));
        }
    }
}
