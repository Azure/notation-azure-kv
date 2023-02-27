package cloud

import (
	"context"
	"crypto/x509"
	"errors"
	"fmt"
	"os"

	"github.com/Azure/azure-sdk-for-go/sdk/azcore"
	"github.com/Azure/azure-sdk-for-go/sdk/azidentity"
	"github.com/Azure/azure-sdk-for-go/sdk/keyvault/azcertificates"
	"github.com/Azure/azure-sdk-for-go/sdk/keyvault/azkeys"
	"github.com/Azure/azure-sdk-for-go/sdk/keyvault/azsecrets"
	"github.com/Azure/notation-azure-kv/internal/crypto"
)

// set of auth errors
var (
	errMsgAuthorizerFromMI  = "authorized from Managed Identity failed, please ensure you have assigned identity to your resource"
	errMsgAuthorizerFromCLI = "authorized from Azure CLI 2.0 failed, please ensure you have logged in using the 'az' command line tool"
	errMsgUnknownAuthorizer = "unknown authorize method, please use a supported authorize method according to the document"
)

// authorizer is the authorize mode used by plugin.
type authorizer string

const (
	// authorizerFromMI auth akv from Managed Identity.
	// The auth order will be:
	// 1. Client credentials
	// 2. Client certificate
	// 3. Username password
	// 4. MSI
	authorizerFromMI authorizer = "AKV_AUTH_FROM_MI"

	// authorizerFromCLI auth akv from Azure cli 2.0.
	authorizerFromCLI authorizer = "AKV_AUTH_FROM_CLI"
)

const (
	// authMethodKey is the environment variable plugin used.
	authMethodKey = "AKV_AUTH_METHOD"

	// defaultAuthMethod is the default auth method if user doesn't provide
	// an environment variable the default value will be authorizerFromCLI.
	defaultAuthMethod = authorizerFromCLI
)

// getAzureClientAuthMethod get authMethod from environment variable
func getAzureClientAuthMethod() authorizer {
	mode := authorizer(os.Getenv(authMethodKey))
	if mode == "" {
		return defaultAuthMethod
	}
	return mode
}

// AzureCredential returns an Azure TokenCredential
func AzureCredential() (credential azcore.TokenCredential, err error) {
	authMethod := getAzureClientAuthMethod()
	switch authMethod {
	case authorizerFromMI:
		credential, err = azidentity.NewManagedIdentityCredential(nil)
		if err != nil {
			err = fmt.Errorf("%s. Original error: %w", errMsgAuthorizerFromMI, err)
		}
	case authorizerFromCLI:
		credential, err = azidentity.NewAzureCLICredential(nil)
		if err != nil {
			err = fmt.Errorf("%s. Original error: %w", errMsgAuthorizerFromCLI, err)
		}
	default:
		err = errors.New(errMsgUnknownAuthorizer)
	}
	return
}

// KeyVault represents a Azure KeyVault Vault.
type KeyVault struct {
	keyClient    *azkeys.Client
	certClient   *azcertificates.Client
	secretClient *azsecrets.Client

	name    string
	version string
}

// NewKeyVault function creates a new instance of KeyVault struct
func NewKeyVault(vaultName, dnsSuffix, keyName, version string) (*KeyVault, error) {
	// get credential
	credential, err := AzureCredential()
	if err != nil {
		return nil, err
	}

	// create cert and key clients
	vaultURL := fmt.Sprintf("https://%s.%s", vaultName, dnsSuffix)
	keyClient, err := azkeys.NewClient(vaultURL, credential, nil)
	if err != nil {
		return nil, err
	}
	certClient, err := azcertificates.NewClient(vaultURL, credential, nil)
	if err != nil {
		return nil, err
	}
	secretClient, err := azsecrets.NewClient(vaultURL, credential, nil)
	if err != nil {
		return nil, err
	}

	return &KeyVault{
		keyClient:    keyClient,
		certClient:   certClient,
		secretClient: secretClient,
		name:         keyName,
		version:      version,
	}, nil
}

// Sign signs the message digest with the algorithm provided.
func (k *KeyVault) Sign(ctx context.Context, algorithm azkeys.JSONWebKeySignatureAlgorithm, digest []byte) ([]byte, error) {
	// Sign the message
	res, err := k.keyClient.Sign(
		ctx,
		k.name,
		k.version,
		azkeys.SignParameters{
			Algorithm: &algorithm,
			Value:     digest,
		},
		nil,
	)
	if err != nil {
		return nil, err
	}

	// Verify the result
	if res.KID == nil {
		return nil, errors.New("azure: nil kid")
	}
	if res.Result == nil {
		return nil, errors.New("azure: invalid server response")
	}
	return res.Result, nil
}

// CertificateChain returns the X.509 certificate chain associated with the key.
func (k *KeyVault) CertificateChain(ctx context.Context) ([]*x509.Certificate, error) {
	secret, err := k.secretClient.GetSecret(ctx, k.name, k.version, nil)
	if err != nil {
		return nil, err
	}
	if secret.Value == nil || secret.ContentType == nil {
		return nil, errors.New("azure: invalid server response")
	}
	return crypto.ParseCertificates([]byte(*secret.Value), *secret.ContentType)
}

// Certificate returns the X.509 certificate associated with the key.
func (k *KeyVault) Certificate(ctx context.Context) (*x509.Certificate, error) {
	cert, err := k.certClient.GetCertificate(ctx, k.name, k.version, nil)
	if err != nil {
		return nil, err
	}
	if cert.KID == nil {
		return nil, errors.New("azure: invalid server response")
	}
	return x509.ParseCertificate(cert.CER)
}
