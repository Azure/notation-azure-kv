package verify

import (
	"crypto/x509"

	"github.com/Azure/notation-azure-kv/pkg/cloud"
	"github.com/Azure/notation-azure-kv/pkg/config"

	jwtazure "github.com/AzureCR/go-jwt-azure"
	"github.com/notaryproject/notation-go-lib"
	"github.com/notaryproject/notation-go-lib/signature/jws"
)

func GetVerifier(k KMSProfileSuite) (notation.Verifier, error) {
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

	roots := x509.NewCertPool()
	roots.AddCert(cert)

	verifier := jws.NewVerifier()
	verifier.VerifyOptions.Roots = roots
	return verifier, nil
}
