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
	cert "github.com/Azure/notation-azure-kv/internal/crypto"
	"github.com/notaryproject/notation-go/plugin/proto"
)

// Sign generates the signature for the given payload using the specified key and hash algorithm.
// It also returns the signing algorithm used, and the certificate chain of the signing key.
func Sign(ctx context.Context, req *proto.GenerateSignatureRequest) (*proto.GenerateSignatureResponse, error) {
	// validate request
	if req == nil || req.KeyID == "" || req.KeySpec == "" || req.Hash == "" {
		return nil, proto.RequestError{
			Code: proto.ErrorCodeValidation,
			Err:  errors.New("invalid request input"),
		}
	}

	// create azure-keyvault client
	key, err := newKey(req.KeyID, req.PluginConfig)
	if err != nil {
		return nil, proto.RequestError{
			Code: proto.ErrorCodeValidation,
			Err:  err,
		}
	}

	// get keySpec
	keySpec, err := proto.DecodeKeySpec(req.KeySpec)
	if err != nil {
		return nil, err
	}

	// get hash and validate hash
	hashName, err := proto.HashAlgorithmFromKeySpec(keySpec)
	if err != nil {
		return nil, err
	}
	if hashName != req.Hash {
		return nil, requestErr(fmt.Errorf("keySpec hash:%v mismatch request hash:%v", hashName, req.Hash))
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
	// complete the cert chain by cert bundle from plugin configuration
	completeCertChain, err := cert.CompleteCertificateChain(req.PluginConfig, certs)
	if err != nil {
		return nil, requestErr(err)
	}
	rawCertChain := make([][]byte, 0, len(completeCertChain))
	for _, cert := range completeCertChain {
		rawCertChain = append(rawCertChain, cert.Raw)
	}

	signatureAlgorithmString, err := proto.EncodeSigningAlgorithm(keySpec.SignatureAlgorithm())
	if err != nil {
		return nil, err
	}
	return &proto.GenerateSignatureResponse{
		KeyID:            req.KeyID,
		Signature:        sig,
		SigningAlgorithm: string(signatureAlgorithmString),
		CertificateChain: rawCertChain,
	}, nil
}

func requestErr(err error) proto.RequestError {
	var code proto.ErrorCode
	var aerr *azure.RequestError
	if errors.As(err, &aerr) {
		switch aerr.StatusCode {
		case http.StatusUnauthorized:
			code = proto.ErrorCodeAccessDenied
		case http.StatusRequestTimeout:
			code = proto.ErrorCodeTimeout
		case http.StatusTooManyRequests:
			code = proto.ErrorCodeThrottled
		default:
			code = proto.ErrorCodeGeneric
		}
	}
	return proto.RequestError{
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

func keySpecToAlg(k proto.KeySpec) keyvault.JSONWebKeySignatureAlgorithm {
	switch k {
	case proto.KeySpecRSA2048:
		return keyvault.PS256
	case proto.KeySpecRSA3072:
		return keyvault.PS384
	case proto.KeySpecRSA4096:
		return keyvault.PS512
	case proto.KeySpecEC256:
		return keyvault.ES256
	case proto.KeySpecEC384:
		return keyvault.ES384
	case proto.KeySpecEC521:
		return keyvault.ES512
	}
	return ""
}
