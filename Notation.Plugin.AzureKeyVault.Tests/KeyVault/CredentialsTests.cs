using Xunit;
using Azure.Core;
using System.Collections.Generic;
using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault.Credential.Tests
{
    public class CredentialsTests
    {
        [Theory]
        [InlineData("default")]
        [InlineData("environment")]
        [InlineData("workloadid")]
        [InlineData("managedid")]
        [InlineData("azurecli")]
        public void GetCredentials_WithValidCredentialType_ReturnsExpectedCredential(string credentialType)
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
            Assert.Equal($"Invalid credential key: {invalidCredentialType}", ex.Message);
        }

        [Fact]
        public void GetCredentials_WithPluginConfig_ReturnsExpectedCredential()
        {
            // Arrange
            var pluginConfig = new Dictionary<string, string>
            {
                { "credential_type", "default" }
            };

            // Act
            var result = Credentials.GetCredentials(pluginConfig);

            // Assert
            Assert.IsAssignableFrom<TokenCredential>(result);
        }
    }
}