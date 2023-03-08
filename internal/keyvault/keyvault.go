package keyvault

import (
	"context"
	"crypto/x509"
	"errors"
	"fmt"
	"net/url"
	"os"
	"strings"

	"github.com/Azure/azure-sdk-for-go/sdk/azcore"
	"github.com/Azure/azure-sdk-for-go/sdk/azidentity"
	"github.com/Azure/azure-sdk-for-go/sdk/keyvault/azcertificates"
	"github.com/Azure/azure-sdk-for-go/sdk/keyvault/azkeys"
	"github.com/Azure/azure-sdk-for-go/sdk/keyvault/azsecrets"
	"github.com/Azure/notation-azure-kv/internal/crypto"
	"github.com/notaryproject/notation-go/plugin/proto"
)

// set of auth errors
var (
	errMsgAuthorizerFromMI  = "authorized from Managed Identity failed, please ensure you have assigned identity to your resource"
	errMsgAuthorizerFromCLI = "authorized from Azure CLI 2.0 failed, please ensure you have logged in using the 'az' command line tool"
	errMsgUnknownAuthorizer = "unknown authorize method, please use a supported authorize method according to the document"
)

// authMethod is the authorize mode used by plugin
type authMethod string

const (
	// authMethodFromMI auth akv from Managed Identity
	authMethodFromMI authMethod = "AKV_AUTH_FROM_MI"

	// authMethodFromCLI auth akv from Azure cli 2.0
	authMethodFromCLI authMethod = "AKV_AUTH_FROM_CLI"
)

const (
	// authMethodKey is the environment variable plugin used
	authMethodKey = "AKV_AUTH_METHOD"

	// defaultAuthMethod is the default auth method if user doesn't provide an environment variable
	// the default value will be authorizerFromCLI
	defaultAuthMethod = authMethodFromCLI
)

type keyClient interface {
	Sign(ctx context.Context, name string, version string, parameters azkeys.SignParameters, options *azkeys.SignOptions) (azkeys.SignResponse, error)
}

type certificateClient interface {
	GetCertificate(ctx context.Context, certificateName string, certificateVersion string, options *azcertificates.GetCertificateOptions) (azcertificates.GetCertificateResponse, error)
}

type secretClient interface {
	GetSecret(ctx context.Context, name string, version string, options *azsecrets.GetSecretOptions) (azsecrets.GetSecretResponse, error)
}

// Certificate includes Sign, CertificateChain, Certificate methods.
type Certificate interface {
	Sign(ctx context.Context, algorithm azkeys.JSONWebKeySignatureAlgorithm, digest []byte) ([]byte, error)
	CertificateChain(ctx context.Context) ([]*x509.Certificate, error)
	Certificate(ctx context.Context) (*x509.Certificate, error)
}

// certificate represents a Azure certificate Vault.
type certificate struct {
	keyClient         keyClient
	certificateClient certificateClient
	secretClient      secretClient

	name    string
	version string
}

// NewCertificateFromID creates a Certificate with given key identifier or
// certificate identifier.
func NewCertificateFromID(id string) (Certificate, error) {
	host, name, version, err := parseIdentifier(id)
	if err != nil {
		return nil, &proto.RequestError{
			Code: proto.ErrorCodeValidation,
			Err:  fmt.Errorf("invalid certificate/key identifier with error: %w. the preferred schema is https://{vaultHost}/[certificates|keys]/{keyName}/{version}", err),
		}
	}
	return NewCertificate(host, name, version)
}

// parseIdentifier parses the key/certificate identifier and returns the
// host, name and version.
func parseIdentifier(id string) (string, string, string, error) {
	u, err := url.Parse(id)
	if err != nil {
		return "", "", "", err
	}

	if u.Scheme != "https" {
		return "", "", "", fmt.Errorf("invalid url schema %q", u.Scheme)
	}

	parts := strings.Split(strings.TrimPrefix(u.Path, "/"), "/")
	if len(parts) != 3 || (parts[0] != "keys" && parts[0] != "certificates") {
		return "", "", "", errors.New("invalid identifier format")
	}
	return u.Host, parts[1], parts[2], nil
}

// NewCertificate function creates a new instance of KeyVault struct
func NewCertificate(vaultHost, keyName, version string) (Certificate, error) {
	// get credential
	credential, err := azureCredential()
	if err != nil {
		return nil, err
	}

	// create cert and key clients
	vaultURL := "https://" + vaultHost
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

	return &certificate{
		keyClient:         keyClient,
		certificateClient: certClient,
		secretClient:      secretClient,
		name:              keyName,
		version:           version,
	}, nil
}

// azureCredential returns an Azure TokenCredential
func azureCredential() (azcore.TokenCredential, error) {
	var (
		credential azcore.TokenCredential
		err        error
	)

	method := getAzureClientAuthMethod()
	switch method {
	case authMethodFromMI:
		credential, err = azidentity.NewManagedIdentityCredential(nil)
		if err != nil {
			err = fmt.Errorf("%s. Original error: %w", errMsgAuthorizerFromMI, err)
		}
	case authMethodFromCLI:
		credential, err = azidentity.NewAzureCLICredential(nil)
		if err != nil {
			err = fmt.Errorf("%s. Original error: %w", errMsgAuthorizerFromCLI, err)
		}
	default:
		err = errors.New(errMsgUnknownAuthorizer)
	}
	return credential, err
}

// getAzureClientAuthMethod get authMethod from environment variable
func getAzureClientAuthMethod() authMethod {
	method := authMethod(os.Getenv(authMethodKey))
	if method == "" {
		return defaultAuthMethod
	}
	return method
}

// Sign signs the message digest with the algorithm provided.
func (k *certificate) Sign(ctx context.Context, algorithm azkeys.JSONWebKeySignatureAlgorithm, digest []byte) ([]byte, error) {
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
func (k *certificate) CertificateChain(ctx context.Context) ([]*x509.Certificate, error) {
	secret, err := k.secretClient.GetSecret(ctx, k.name, k.version, nil)
	if err != nil {
		return nil, err
	}
	if secret.Value == nil || secret.ContentType == nil {
		return nil, errors.New("azure: invalid server response")
	}
	// For PKCS12 format, secret.Value is base64 encoded data.
	// for PEM format, secret.Value is the raw data in PEM format.
	return crypto.ParseCertificates([]byte(*secret.Value), *secret.ContentType)
}

// Certificate returns the X.509 certificate associated with the key.
func (k *certificate) Certificate(ctx context.Context) (*x509.Certificate, error) {
	cert, err := k.certificateClient.GetCertificate(ctx, k.name, k.version, nil)
	if err != nil {
		return nil, err
	}
	if cert.KID == nil {
		return nil, errors.New("azure: invalid server response")
	}
	return x509.ParseCertificate(cert.CER)
}
