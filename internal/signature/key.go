package signature

import (
	"context"
	"crypto/x509"
	"errors"

	"github.com/Azure/notation-azure-kv/internal/cloud"
	"github.com/notaryproject/notation-go"
	"github.com/notaryproject/notation-go/plugin"
)

func newKey(keyID string, pluginConfig map[string]string) (*cloud.Key, error) {
	client, err := cloud.NewAzureClient()
	if err != nil {
		return nil, err
	}
	if vaultName := pluginConfig["vaultName"]; vaultName != "" {
		keyVersion := pluginConfig["keyVersion"]
		return cloud.NewKey(client, vaultName, keyID, keyVersion)
	}
	return cloud.NewKeyFromID(client, keyID)
}

func Key(ctx context.Context, req *plugin.DescribeKeyRequest) (*plugin.DescribeKeyResponse, error) {
	if req == nil || req.KeyID == "" {
		return nil, plugin.RequestError{
			Code: plugin.ErrorCodeValidation,
			Err:  errors.New("invalid request input"),
		}
	}
	key, err := newKey(req.KeyID, req.PluginConfig)
	if err != nil {
		return nil, plugin.RequestError{
			Code: plugin.ErrorCodeValidation,
			Err:  err,
		}
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

func certToKeySpec(alg x509.SignatureAlgorithm) notation.KeySpec {
	switch alg {
	case x509.SHA256WithRSAPSS, x509.SHA256WithRSA:
		return notation.RSA_2048
	case x509.SHA384WithRSAPSS, x509.SHA384WithRSA:
		return notation.RSA_3072
	case x509.SHA512WithRSAPSS, x509.SHA512WithRSA:
		return notation.RSA_4096
	case x509.ECDSAWithSHA256:
		return notation.EC_256
	case x509.ECDSAWithSHA384:
		return notation.EC_384
	case x509.ECDSAWithSHA512:
		return notation.EC_512
	}
	return ""
}
