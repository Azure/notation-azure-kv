package jws

import (
	"crypto/x509"
	"errors"

	"github.com/Azure/notation-azure-kv/pkg/cloud"
	"github.com/Azure/notation-azure-kv/pkg/config"
	jwtazure "github.com/AzureCR/go-jwt-azure"
	"github.com/notaryproject/notation-go/plugin"
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
	var keySpec string
	switch cert.SignatureAlgorithm {
	case x509.SHA256WithRSAPSS, x509.SHA256WithRSA:
		keySpec = "RSA_2048"
	case x509.SHA384WithRSAPSS, x509.SHA384WithRSA:
		keySpec = "RSA_3072"
	case x509.SHA512WithRSAPSS, x509.SHA512WithRSA:
		keySpec = "RSA_4096"
	case x509.ECDSAWithSHA256:
		keySpec = "EC_256"
	case x509.ECDSAWithSHA384:
		keySpec = "EC_384"
	case x509.ECDSAWithSHA512:
		keySpec = "EC_512"
	default:
		return nil, errors.New("unrecognized key spec: " + cert.SignatureAlgorithm.String())
	}
	return &plugin.DescribeKeyResponse{
		KeyID:   req.KeyID,
		KeySpec: keySpec,
	}, nil
}
