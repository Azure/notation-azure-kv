using System;
using Xunit;

namespace Notation.Plugin.Protocol.Tests
{
    public class DescribeKeyTests
    {
        [Fact]
        public void DescribeKeyRequest_ValidParameters()
        {
            // Arrange
            string contractVersion = "1.0";
            string keyId = "test-key-id";

            // Act
            DescribeKeyRequest request = new DescribeKeyRequest(contractVersion, keyId);

            // Assert
            Assert.Equal(contractVersion, request.ContractVersion);
            Assert.Equal(keyId, request.KeyId);
        }

        [Theory]
        [InlineData(null, "test-key-id")]
        [InlineData("", "test-key-id")]
        [InlineData("1.0", null)]
        [InlineData("1.0", "")]
        public void DescribeKeyRequest_InvalidParameters_ThrowsException(string contractVersion, string keyId)
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DescribeKeyRequest(contractVersion, keyId));
        }

        [Fact]
        public void DescribeKeyRequest_UnsupportedContractVersion_ThrowsException()
        {
            // Arrange
            string contractVersion = "2.0";
            string keyId = "test-key-id";

            // Act & Assert
            Assert.Throws<ValidationException>(() => new DescribeKeyRequest(contractVersion, keyId));
        }

        [Fact]
        public void DescribeKeyResponse_ValidParameters()
        {
            // Arrange
            string keyId = "test-key-id";
            string keySpec = "RSA-2048";

            // Act
            DescribeKeyResponse response = new DescribeKeyResponse(keyId, keySpec);
            var json = response.ToJson();

            // Assert
            Assert.Equal(keyId, response.KeyId);
            Assert.Equal(keySpec, response.KeySpec);
            Assert.Equal("{\"keyId\":\"test-key-id\",\"keySpec\":\"RSA-2048\"}", json);
        }

        [Theory]
        [InlineData(null, "RSA-2048")]
        [InlineData("", "RSA-2048")]
        [InlineData("test-key-id", null)]
        [InlineData("test-key-id", "")]
        public void DescribeKeyResponse_InvalidParameters_ThrowsException(string keyId, string keySpec)
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DescribeKeyResponse(keyId, keySpec));
        }
    }
}
