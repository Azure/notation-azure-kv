package signature

import (
	"context"
	"crypto"
	"encoding/base64"
	"errors"
	"net/http"

	// Make required hashers available.
	_ "crypto/sha256"
	_ "crypto/sha512"

	"github.com/Azure/azure-sdk-for-go/services/keyvault/v7.1/keyvault"
	"github.com/Azure/go-autorest/autorest/azure"

	"github.com/notaryproject/notation-go/plugin"
)

func Sign(ctx context.Context, req *plugin.GenerateSignatureRequest) (*plugin.GenerateSignatureResponse, error) {
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

	// Digest.
	hashed, err := computeHash(keySpec.HashFunc(), []byte(req.Payload))
	if err != nil {
		return nil, err
	}

	// Sign.
	alg := keySpecToAlg(keySpec)
	sig, err := key.Sign(ctx, alg, hashed)
	if err != nil {
		return nil, requestErr(err)
	}

	return &plugin.GenerateSignatureResponse{
		KeyID:            req.KeyID,
		Signature:        base64.RawStdEncoding.EncodeToString(sig),
		SigningAlgorithm: string(alg),
		CertificateChain: []string{
			base64.RawStdEncoding.EncodeToString(cert.Raw),
		},
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

func keySpecToAlg(k plugin.KeySpec) keyvault.JSONWebKeySignatureAlgorithm {
	switch k {
	case plugin.RSA_2048:
		return keyvault.PS256
	case plugin.RSA_3072:
		return keyvault.PS384
	case plugin.RSA_4096:
		return keyvault.PS512
	case plugin.EC_256:
		return keyvault.ES256
	case plugin.EC_384:
		return keyvault.ES384
	case plugin.EC_512:
		return keyvault.ES512
	}
	return ""
}
