package cloud

import (
	"strings"

	"github.com/Azure/notation-azure-kv/pkg/config"

	"github.com/Azure/azure-sdk-for-go/services/keyvault/v7.1/keyvault"
	"github.com/Azure/go-autorest/autorest"
	"github.com/Azure/go-autorest/autorest/adal"
	"github.com/Azure/go-autorest/autorest/azure"
)

// NewAzureClient returns a new Azure Key Vault client
func NewAzureClient(c *config.Config) (*keyvault.BaseClient, error) {
	// TODO: add support for other clouds
	azureEnv := azure.PublicCloud
	oauthConfig, err := adal.NewOAuthConfig(azureEnv.ActiveDirectoryEndpoint, c.TenantID)
	if err != nil {
		return nil, err
	}
	spt, err := adal.NewServicePrincipalToken(*oauthConfig, c.ClientID, c.ClientSecret, strings.TrimSuffix(azureEnv.KeyVaultEndpoint, "/"))
	if err != nil {
		return nil, err
	}

	client := keyvault.New()
	client.Authorizer = autorest.NewBearerAuthorizer(spt)
	return &client, nil
}
