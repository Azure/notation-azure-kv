using Azure.Core;
using Azure.Identity;
using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault.Credential
{
    public class Credentials
    {
        /// <summary>
        /// Credential type key name in plugin config.
        /// </summary>
        public const string CredentialTypeKey = "credential_type";
        /// <summary>
        /// Default credential name.
        /// </summary>
        public const string DefaultCredentialName = "default";
        /// <summary>
        /// Environment credential name.
        /// </summary>
        public const string EnvironmentCredentialName = "environment";
        /// <summary>
        /// Workload identity credential name.
        /// </summary>
        public const string WorkloadIdentityCredentialName = "workloadidentity";
        /// <summary>
        /// Managed identity credential name.
        /// </summary>
        public const string ManagedIdentityCredentialName = "managedidentity";
        /// <summary>
        /// Azure CLI credential name.
        /// </summary>
        public const string AzureCliCredentialName = "azurecli";

        /// <summary>
        /// Get the credential based on the credential type.
        /// </summary>
        public static TokenCredential GetCredentials(string credentialType)
        {
            credentialType = credentialType.ToLower();
            switch (credentialType)
            {
                case DefaultCredentialName:
                    return new DefaultAzureCredential();
                case EnvironmentCredentialName:
                    return new EnvironmentCredential();
                case WorkloadIdentityCredentialName:
                    return new WorkloadIdentityCredential();
                case ManagedIdentityCredentialName:
                    return new ManagedIdentityCredential();
                case AzureCliCredentialName:
                    return new AzureCliCredential();
                default:
                    throw new ValidationException($"Invalid credential key: {credentialType}");
            }
        }

        /// <summary>
        /// Get the credential based on the plugin config.
        /// </summary>
        public static TokenCredential GetCredentials(Dictionary<string, string>? pluginConfig)
        {
            var credentialName = pluginConfig?.GetValueOrDefault(CredentialTypeKey, DefaultCredentialName) ??
                                    DefaultCredentialName;
            return GetCredentials(credentialName);
        }
    }
}