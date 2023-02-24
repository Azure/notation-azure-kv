package cloud

import (
	"context"
	"crypto/x509"
	"encoding/base64"
	"errors"
	"fmt"
	"os"

	"github.com/Azure/azure-sdk-for-go/sdk/azcore"
	"github.com/Azure/azure-sdk-for-go/sdk/azidentity"
	"github.com/Azure/azure-sdk-for-go/sdk/keyvault/azcertificates"
	"github.com/Azure/azure-sdk-for-go/sdk/keyvault/azkeys"
	"github.com/Azure/notation-azure-kv/internal/crypto"
)

// clientAuthMode is the authorize mode used by plugin
type clientAuthMethod string

// set of auth errors
var (
	errMsgAuthorizerFromMI  = "authorized from Managed Identity failed, please ensure you have assigned identity to your resource"
	errMsgAuthorizerFromCLI = "authorized from Azure CLI 2.0 failed, please ensure you have logged in using the 'az' command line tool"
	errMsgUnknownAuthorizer = "unknown authorize method, please use a supported authorize method according to the document"
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

func AzureCredential() (credential azcore.TokenCredential, err error) {
	authMethod := getAzureClientAuthMethod()
	switch authMethod {
	case authorizerFromMI:
		credential, err = azidentity.NewManagedIdentityCredential(nil)
	case authorizerFromCLI:
		credential, err = azidentity.NewAzureCLICredential(nil)
	}
	return
}

// KeyVault represents a remote key in the Azure KeyVault Vault.
type KeyVault struct {
	KeyClient  *azkeys.Client
	CertClient *azcertificates.Client

	name    string
	version string
}

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

	return &KeyVault{
		KeyClient:  keyClient,
		CertClient: certClient,
		name:       keyName,
		version:    version,
	}, nil
}

// Sign signs the message digest with the algorithm provided.
func (k *KeyVault) Sign(ctx context.Context, algorithm azkeys.JSONWebKeySignatureAlgorithm, digest []byte) ([]byte, error) {
	// Prepare the message
	value := base64.RawURLEncoding.EncodeToString(digest)

	// Sign the message
	res, err := k.KeyClient.Sign(
		ctx,
		k.name,
		k.version,
		azkeys.SignParameters{
			Algorithm: &algorithm,
			Value:     []byte(value),
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
	return base64.RawURLEncoding.DecodeString(string(res.Result))
}

// Certificate returns the X.509 certificate chain associated with the key.
func (k *KeyVault) Certificate(ctx context.Context) ([]*x509.Certificate, error) {
	cert, err := k.CertClient.GetCertificate(ctx, k.name, k.version, nil)
	if err != nil {
		return nil, err
	}
	return crypto.ParseCertificates(cert.CER, *cert.ContentType)
}
