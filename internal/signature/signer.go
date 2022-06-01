package signature

import (
	"context"
	"crypto"
	"errors"
	"net/http"

	// Make required hashers available.
	_ "crypto/sha256"
	_ "crypto/sha512"

	"github.com/Azure/azure-sdk-for-go/services/keyvault/v7.1/keyvault"
	"github.com/Azure/go-autorest/autorest/azure"

	"github.com/notaryproject/notation-go"
	"github.com/notaryproject/notation-go/plugin"
)

func Sign(ctx context.Context, req *plugin.GenerateSignatureRequest) (*plugin.GenerateSignatureResponse, error) {
	if req == nil || req.KeyID == "" || req.KeySpec == "" || req.Hash == "" {
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

	alg := keySpecToAlg(req.KeySpec)
	if alg == "" {
		return nil, errors.New("unrecognized key spec: " + string(req.KeySpec))
	}

	// Digest.
	hashed, err := computeHash(req.Hash.HashFunc(), req.Payload)
	if err != nil {
		return nil, err
	}

	// Sign.
	sig, err := key.Sign(ctx, alg, hashed)
	if err != nil {
		return nil, requestErr(err)
	}

	return &plugin.GenerateSignatureResponse{
		KeyID:            req.KeyID,
		Signature:        sig,
		SigningAlgorithm: req.KeySpec.SignatureAlgorithm(),
		CertificateChain: [][]byte{cert.Raw},
	}, nil
}

func requestErr(err error) plugin.RequestError {
	var code plugin.ErrorCode
	var aerr *azure.RequestError
	if errors.As(err, &aerr) {
		switch aerr.StatusCode {
		case http.StatusUnauthorized:
			code = plugin.ErrorCodeAccessDenied
		case http.StatusRequestTimeout:
			code = plugin.ErrorCodeTimeout
		case http.StatusTooManyRequests:
			code = plugin.ErrorCodeThrottled
		default:
			code = plugin.ErrorCodeGeneric
		}
	}
	return plugin.RequestError{
		Code: code,
		Err:  err,
	}
}

// computeHash computes the digest of the message with the given hash algorithm.
func computeHash(hash crypto.Hash, message []byte) ([]byte, error) {
	if !hash.Available() {
		return nil, errors.New("unavailable hash function: " + hash.String())
	}
	h := hash.New()
	if _, err := h.Write(message); err != nil {
		return nil, err
	}
	return h.Sum(nil), nil
}

func keySpecToAlg(k notation.KeySpec) keyvault.JSONWebKeySignatureAlgorithm {
	switch k {
	case notation.RSA_2048:
		return keyvault.PS256
	case notation.RSA_3072:
		return keyvault.PS384
	case notation.RSA_4096:
		return keyvault.PS512
	case notation.EC_256:
		return keyvault.ES256
	case notation.EC_384:
		return keyvault.ES384
	case notation.EC_512:
		return keyvault.ES512
	}
	return ""
}
