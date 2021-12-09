package cloud

import (
	"strings"

	"github.com/Azure/notation-azure-kv/pkg/config"

	"github.com/Azure/azure-sdk-for-go/services/keyvault/v7.1/keyvault"
	"github.com/Azure/azure-sdk-for-go/services/keyvault/v7.1/keyvault/keyvaultapi"
	"github.com/Azure/go-autorest/autorest"
	"github.com/Azure/go-autorest/autorest/adal"
	"github.com/Azure/go-autorest/autorest/azure"
	"github.com/pkg/errors"
)

// NewAzureClient returns a new Azure Key Vault client
func NewAzureClient(creds config.Credentials) (keyvaultapi.BaseClientAPI, error) {
	if creds.ClientID == "" || creds.ClientSecret == "" || creds.TenantID == "" {
		return nil, errors.New("missing credentials")
	}
	// TODO: add support for other clouds
	azureEnv := azure.PublicCloud
	oauthConfig, err := adal.NewOAuthConfig(azureEnv.ActiveDirectoryEndpoint, creds.TenantID)
	if err != nil {
		return nil, err
	}
	spt, err := adal.NewServicePrincipalToken(*oauthConfig, creds.ClientID, creds.ClientSecret, strings.TrimSuffix(azureEnv.KeyVaultEndpoint, "/"))
	if err != nil {
		return nil, err
	}

	client := keyvault.New()
	client.Authorizer = autorest.NewBearerAuthorizer(spt)
	return client, nil
}
