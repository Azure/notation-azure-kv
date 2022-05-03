package jws

import (
	"crypto/x509"

	"github.com/Azure/notation-azure-kv/pkg/cloud"
	"github.com/Azure/notation-azure-kv/pkg/config"
	jwtazure "github.com/AzureCR/go-jwt-azure"
	"github.com/notaryproject/notation-go/plugin"
	"github.com/notaryproject/notation-go/signature/jws"
	"github.com/pkg/errors"
)

func newKey(keyID string) (*jwtazure.Key, error) {
	cfg, err := config.ParseConfig()
	if err != nil {
		return nil, err
	}
	client, err := cloud.NewAzureClient(cfg)
	if err != nil {
		return nil, err
	}
	return jwtazure.NewKey(client, keyID)
}

func newCert(keyID string) (*x509.Certificate, error) {
	key, err := newKey(keyID)
	if err != nil {
		return nil, err
	}
	return key.Certificate()
}

func Key(req *plugin.DescribeKeyRequest) (*plugin.DescribeKeyResponse, error) {
	if req == nil || req.KeyID == "" {
		return nil, plugin.RequestError{
			Code: plugin.ErrorCodeValidation,
			Err:  errors.New("invalid request input"),
		}
	}
	cert, err := newCert(req.KeyID)
	if err != nil {
		return nil, requestErr(err)
	}
	method, err := jws.SigningMethodFromKey(cert.PublicKey)
	if err != nil {
		return nil, errors.Errorf("unrecognized key method: %w", err)
	}
	return &plugin.DescribeKeyResponse{
		KeyID:     req.KeyID,
		Algorithm: method.Alg(),
	}, nil
}
