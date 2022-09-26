package signature

import (
	"context"
	"crypto"
	"errors"
	"fmt"
	"net/http"

	// Make required hashers available.
	_ "crypto/sha256"
	_ "crypto/sha512"

	"github.com/Azure/azure-sdk-for-go/services/keyvault/v7.1/keyvault"
	"github.com/Azure/go-autorest/autorest/azure"
	"github.com/notaryproject/notation-go/plugin"
)

func Sign(ctx context.Context, req *plugin.GenerateSignatureRequest) (*plugin.GenerateSignatureResponse, error) {
	// validate request
	if req == nil || req.KeyID == "" || req.KeySpec == "" || req.Hash == "" {
		return nil, plugin.RequestError{
			Code: plugin.ErrorCodeValidation,
			Err:  errors.New("invalid request input"),
		}
	}

	// create azure-keyvault client
	key, err := newKey(req.KeyID, req.PluginConfig)
	if err != nil {
		return nil, plugin.RequestError{
			Code: plugin.ErrorCodeValidation,
			Err:  err,
		}
	}

	// get keySpec
	keySpec, err := plugin.ParseKeySpec(req.KeySpec)
	if err != nil {
		return nil, err
	}

	// get hash and validate hash
	if name := plugin.KeySpecHashString(keySpec); name != req.Hash {
		return nil, requestErr(fmt.Errorf("keySpec hash:%v mismatch request hash:%v", name, req.Hash))
	}

	// get signing alg
	signAlg := keySpecToAlg(req.KeySpec)
	if signAlg == "" {
		return nil, errors.New("unrecognized key spec: " + string(req.KeySpec))
	}

	// Digest.
	hashed, err := computeHash(keySpec.SignatureAlgorithm().Hash(), req.Payload)
	if err != nil {
		return nil, err
	}

	// Sign.
	sig, err := key.Sign(ctx, signAlg, hashed)
	if err != nil {
		return nil, requestErr(err)
	}

	// get certificate
	certs, err := key.CertificateChain(ctx)
	if err != nil {
		return nil, requestErr(err)
	}
	certChain := make([][]byte, 0, len(certs))
	for _, cert := range certs {
		certChain = append(certChain, cert.Raw)
	}
	return &plugin.GenerateSignatureResponse{
		KeyID:            req.KeyID,
		Signature:        sig,
		SigningAlgorithm: plugin.SigningAlgorithmString(keySpec.SignatureAlgorithm()),
		CertificateChain: certChain,
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

func keySpecToAlg(k string) keyvault.JSONWebKeySignatureAlgorithm {
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
	case plugin.EC_521:
		return keyvault.ES512
	}
	return ""
}
