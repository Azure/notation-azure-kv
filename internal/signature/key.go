package signature

import (
	"context"
	"crypto/x509"
	"errors"

	"github.com/Azure/notation-azure-kv/internal/cloud"
	"github.com/notaryproject/notation-go/spec/plugin"
	"github.com/notaryproject/notation-go/spec/signature"
)

func newKey(keyID string) (*cloud.Key, error) {
	client, err := cloud.NewAzureClient()
	if err != nil {
		return nil, err
	}
	return cloud.NewKey(client, keyID)
}

func Key(ctx context.Context, req *plugin.DescribeKeyRequest) (*plugin.DescribeKeyResponse, error) {
	if req == nil || req.KeyID == "" {
		return nil, plugin.RequestError{
			Code: plugin.ErrorCodeValidation,
			Err:  errors.New("invalid request input"),
		}
	}
	key, err := newKey(req.KeyID)
	if err != nil {
		return nil, err
	}
	cert, err := key.Certificate(ctx)
	if err != nil {
		return nil, requestErr(err)
	}
	keySpec := certToKeySpec(cert.SignatureAlgorithm)
	if keySpec == "" {
		return nil, errors.New("unrecognized key spec: " + cert.SignatureAlgorithm.String())
	}
	return &plugin.DescribeKeyResponse{
		KeyID:   req.KeyID,
		KeySpec: keySpec,
	}, nil
}

func certToKeySpec(alg x509.SignatureAlgorithm) signature.KeyType {
	switch alg {
	case x509.SHA256WithRSAPSS, x509.SHA256WithRSA:
		return signature.RSA_2048
	case x509.SHA384WithRSAPSS, x509.SHA384WithRSA:
		return signature.RSA_3072
	case x509.SHA512WithRSAPSS, x509.SHA512WithRSA:
		return signature.RSA_4096
	case x509.ECDSAWithSHA256:
		return signature.EC_256
	case x509.ECDSAWithSHA384:
		return signature.EC_384
	case x509.ECDSAWithSHA512:
		return signature.EC_512
	}
	return ""
}
