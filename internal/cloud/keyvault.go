package cloud

import (
	"context"
	"crypto/x509"
	"encoding/base64"
	"errors"
	"fmt"
	"net/url"
	"os"
	"strings"

	"github.com/Azure/azure-sdk-for-go/services/keyvault/auth"
	"github.com/Azure/azure-sdk-for-go/services/keyvault/v7.1/keyvault"
	"github.com/Azure/go-autorest/autorest"
	"github.com/Azure/go-autorest/autorest/azure"
	"github.com/Azure/notation-azure-kv/internal/crypto"
)

// clientAuthMode is the authorize mode used by plugin
type clientAuthMethod string

// set of auth errors
var (
	errAuthorizerFromMI  = errors.New("authorized from Managed Identity failed, please ensure you have assigned identity to your resource")
	errAuthorizerFromCLI = errors.New("authorized from Azure CLI 2.0 failed, please ensure you have logged in using the 'az' command line tool")
	errUnknownAuthorizer = errors.New("unknown authorize method, please use a supported authorize method according to the document")
)

const (
	// authMethodKey is the environment variable plugin used
	authMethodKey = "AKV_AUTH_METHOD"
	// authorizerFromMI auth akv from Managed Identity
	// The auth order will be:
	// 1. Client credentials
	// 2. Client certificate
	// 3. Username password
	// 4. MSI
	authorizerFromMI clientAuthMethod = "AKV_AUTH_FROM_MI"

	// authorizerFromCLI auth akv from Azure cli 2.0
	authorizerFromCLI clientAuthMethod = "AKV_AUTH_FROM_CLI"

	// defaultAuthMethod is the default auth method if user doesn't provide an environment variable
	// the default value will be authorizerFromCLI
	defaultAuthMethod = authorizerFromCLI
)

// getAzureClientAuthMethod get authMethod from environment variable
func getAzureClientAuthMethod() clientAuthMethod {
	mode := clientAuthMethod(os.Getenv(authMethodKey))
	if mode == "" {
		return defaultAuthMethod
	}
	return mode
}

// NewAzureClient returns a new Azure Key Vault client
// By default, the authorizer will be from Managed Identity
// If user sets AKV_AUTH_METHOD to AKV_AUTH_FROM_CLI, it will use Azure CLI instead
func NewAzureClient() (*keyvault.BaseClient, error) {
	var (
		authorizer autorest.Authorizer
		err        error
	)
	authMethod := getAzureClientAuthMethod()
	switch authMethod {
	case authorizerFromMI:
		authorizer, err = auth.NewAuthorizerFromEnvironment()
		if err != nil {
			return nil, fmt.Errorf("%w, origin error: %s", errAuthorizerFromMI, err.Error())
		}
	case authorizerFromCLI:
		authorizer, err = auth.NewAuthorizerFromCLI()
		if err != nil {
			return nil, fmt.Errorf("%w, origin error: %s", errAuthorizerFromCLI, err.Error())
		}
	default:
		return nil, errUnknownAuthorizer
	}
	client := keyvault.New()
	client.Authorizer = authorizer
	return &client, nil
}

// Key represents a remote key in the Azure Key Vault.
type Key struct {
	Client *keyvault.BaseClient

	vaultBaseURL string
	name         string
	version      string
}

// NewKeyFromID create a remote key referenced by a key identifier.
func NewKeyFromID(client *keyvault.BaseClient, keyID string) (*Key, error) {
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
			vaultBaseURL: keyURL.Scheme + "://" + keyURL.Host,
			name:         parts[1],
			version:      parts[2],
		},
		nil
}

// NewKey create a remote key reference.
func NewKey(client *keyvault.BaseClient, vaultName, keyName, keyVersion string) (*Key, error) {
	dnssuffix := os.Getenv("AZURE_KEYVAULT_DNSSUFFIX")
	if dnssuffix == "" {
		var env azure.Environment
		if envName := os.Getenv("AZURE_ENVIRONMENT"); envName == "" {
			env = azure.PublicCloud
		} else {
			var err error
			env, err = azure.EnvironmentFromName(envName)
			if err != nil {
				return nil, err
			}
		}
		dnssuffix = env.KeyVaultDNSSuffix
	}
	return &Key{
		Client:       client,
		vaultBaseURL: "https://" + vaultName + "." + dnssuffix,
		name:         keyName,
		version:      keyVersion,
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
	if res.Kid == nil {
		return nil, errors.New("azure: nil kid")
	}
	if res.Result == nil {
		return nil, errors.New("azure: invalid server response")
	}
	return base64.RawURLEncoding.DecodeString(*res.Result)
}

// Certificate returns the X.509 certificate chain associated with the key.
func (k *Key) CertificateChain(ctx context.Context) ([]*x509.Certificate, error) {
	// Need a certificate chain to pass the notation validation
	// GetCertificate only returns the leaf certificate
	// So we need a GetSecret to get the full certificate chain
	res, err := k.Client.GetSecret(
		ctx,
		k.vaultBaseURL,
		k.name,
		k.version,
	)
	if err != nil {
		return nil, err
	}
	if res.Value == nil || res.ContentType == nil {
		return nil, errors.New("azure: invalid server response")
	}
	return crypto.ParseCertificates([]byte(*res.Value), *res.ContentType)
}
