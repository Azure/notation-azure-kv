package cloud

import (
	"context"
	"crypto/x509"
	"encoding/base64"
	"errors"
	"fmt"
	"net/url"
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

// Key represents a remote key in the Azure Key Vault.
type Key struct {
	Client *keyvault.BaseClient

	id           string
	vaultBaseURL string
	name         string
	version      string
}

// NewKey create a remote key referenced by a key identifier.
func NewKey(client *keyvault.BaseClient, keyID string) (*Key, error) {
	keyURL, err := url.Parse(keyID)
	if err != nil {
		return nil, fmt.Errorf("invalid keyID: %q is not a valid URI", keyID)
	}

	parts := strings.Split(strings.TrimPrefix(keyURL.Path, "/"), "/")
	if len(parts) != 3 || parts[0] != "keys" {
		return nil, fmt.Errorf("invalid keyID: the specified uri %q, does to match the specified format \"{vault}/keys/{name}/{version?}\"", keyID)
	}

	return &Key{
		Client:       client,
		id:           keyID,
		vaultBaseURL: keyURL.Scheme + "://" + keyURL.Host,
		name:         parts[1],
		version:      parts[2],
	}, nil
}

// Sign signs the message digest with the algorithm provided.
func (k *Key) Sign(ctx context.Context, algorithm keyvault.JSONWebKeySignatureAlgorithm, digest []byte) ([]byte, error) {
	// Prepare the message
	value := base64.RawURLEncoding.EncodeToString(digest)

	// Sign the message
	res, err := k.Client.Sign(
		ctx,
		k.vaultBaseURL,
		k.name,
		k.version,
		keyvault.KeySignParameters{
			Algorithm: algorithm,
			Value:     &value,
		},
	)
	if err != nil {
		return nil, err
	}

	// Verify the result
	if res.Kid == nil || *res.Kid != k.id {
		return nil, errors.New("azure: response key id mismatch")
	}
	if res.Result == nil {
		return nil, errors.New("azure: invalid server response")
	}
	return base64.RawURLEncoding.DecodeString(*res.Result)
}

// Certificate returns the X.509 certificate associated with the key.
func (k *Key) Certificate(ctx context.Context) (*x509.Certificate, error) {
	res, err := k.Client.GetCertificate(
		ctx,
		k.vaultBaseURL,
		k.name,
		k.version,
	)
	if err != nil {
		return nil, err
	}
	if res.Cer == nil {
		return nil, errors.New("azure: invalid server response")
	}
	return x509.ParseCertificate(*res.Cer)
}
