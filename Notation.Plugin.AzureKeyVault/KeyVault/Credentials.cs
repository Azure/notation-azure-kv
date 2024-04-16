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
        /// Environment credential name.
        /// </summary>
        public const string EnvironmentCredentialName = "environment";
        /// <summary>
        /// Workload identity credential name.
        /// </summary>
        public const string WorkloadIdentityCredentialName = "workloadid";
        /// <summary>
        /// Managed identity credential name.
        /// </summary>
        public const string ManagedIdentityCredentialName = "managedid";
        /// <summary>
        /// Azure CLI credential name.
        /// </summary>
        public const string AzureCliCredentialName = "azurecli";

        /// <summary>
        /// Get the credential based on the credential type.
        /// </summary>
        public static TokenCredential GetCredentials(string? credentialType)
        {
            if (credentialType == null)
            {
                return new DefaultAzureCredential();
            }

            credentialType = credentialType.ToLower();
            switch (credentialType)
            {
                case EnvironmentCredentialName:
                    return new EnvironmentCredential();
                case WorkloadIdentityCredentialName:
                    return new WorkloadIdentityCredential();
                case ManagedIdentityCredentialName:
                    return new ManagedIdentityCredential();
                case AzureCliCredentialName:
                    return new AzureCliCredential();
                default:
                    throw new ValidationException($"Invalid credential type: {credentialType}");
            }
        }

        /// <summary>
        /// Get the credential based on the plugin config.
        /// </summary>
        public static TokenCredential GetCredentials(Dictionary<string, string>? pluginConfig)
        {
            string? credentialType = null;
            pluginConfig?.TryGetValue(CredentialTypeKey, out credentialType);
            return GetCredentials(credentialType);
        }
    }
}
