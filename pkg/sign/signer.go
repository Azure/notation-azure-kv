package sign

import (
	"crypto/x509"

	"github.com/Azure/notation-akv/pkg/cloud"
	"github.com/Azure/notation-akv/pkg/config"

	"github.com/Azure/azure-sdk-for-go/services/keyvault/v7.1/keyvault"
	jwtazure "github.com/AzureCR/go-jwt-azure"
	"github.com/notaryproject/notation-go-lib"
	"github.com/notaryproject/notation-go-lib/signature/jws"
	"github.com/pkg/errors"
)

// GetSigner returns an Azure Key Vault signer
func GetSigner(k KMSKeySuite) (notation.Signer, error) {
	// core process
	cfg, err := config.Load()
	if err != nil {
		return nil, err
	}
	client, err := cloud.NewAzureClient(cfg.Credentials)
	if err != nil {
		return nil, err
	}
	key, err := jwtazure.NewKey(client, k.ID)
	if err != nil {
		return nil, err
	}
	cert, err := key.Certificate()
	if err != nil {
		return nil, err
	}

	// get corresponding signing method and override with Azure implementation
	method, err := jws.SigningMethodFromKey(cert.PublicKey)
	if err != nil {
		return nil, err
	}
	alg := keyvault.JSONWebKeySignatureAlgorithm(method.Alg())
	// TODO: check if alg is supported by Azure Key Vault
	method, ok := jwtazure.SigningMethods[alg]
	if !ok {
		return nil, errors.Errorf("unrecognized signing method: %v", alg)
	}

	signer, err := jws.NewSignerWithCertificateChain(method, key, []*x509.Certificate{cert})
	if err != nil {
		return nil, err
	}
	// TODO: check if timestamping is supported by Azure Key Vault
	return signer, nil
}
