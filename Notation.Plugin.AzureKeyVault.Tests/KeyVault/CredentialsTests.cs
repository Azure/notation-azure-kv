using Xunit;
using Azure.Core;
using System.Collections.Generic;
using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault.Credential.Tests
{
    public class CredentialsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("environment")]
        [InlineData("workloadid")]
        [InlineData("managedid")]
        [InlineData("azurecli")]
        public void GetCredentials_WithValidCredentialType_ReturnsExpectedCredential(string? credentialType)
        {
            // Act
            var result = Credentials.GetCredentials(credentialType);

            // Assert
            Assert.IsAssignableFrom<TokenCredential>(result);
        }

        [Fact]
        public void GetCredentials_WithInvalidCredentialType_ThrowsValidationException()
        {
            // Arrange
            string invalidCredentialType = "invalid";

            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() => Credentials.GetCredentials(invalidCredentialType));
            Assert.Equal($"Invalid credential type: {invalidCredentialType}", ex.Message);
        }
    }
}