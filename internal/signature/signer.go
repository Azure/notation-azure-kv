package signature

import (
	"context"
	"crypto"
	"errors"
	"fmt"

	// Make required hashers available.
	_ "crypto/sha256"
	_ "crypto/sha512"
	"crypto/x509"

	"github.com/Azure/azure-sdk-for-go/sdk/keyvault/azkeys"
	certutils "github.com/Azure/notation-azure-kv/internal/crypto"
	"github.com/Azure/notation-azure-kv/internal/keyvault"
	"github.com/notaryproject/notation-go/plugin/proto"
)

// Sign generates the signature for the given payload using the specified key and hash algorithm.
// It also returns the signing algorithm used, and the certificate chain of the signing key.
func Sign(ctx context.Context, req *proto.GenerateSignatureRequest) (*proto.GenerateSignatureResponse, error) {
	// validate request
	if req == nil || req.KeyID == "" || req.KeySpec == "" || req.Hash == "" {
		return nil, &proto.RequestError{
			Code: proto.ErrorCodeValidation,
			Err:  errors.New("invalid request input"),
		}
	}

	// create azure-keyvault client
	kv, err := keyvault.NewCertificateFromID(req.KeyID)
	if err != nil {
		return nil, &proto.RequestError{
			Code: proto.ErrorCodeValidation,
			Err:  err,
		}
	}

	// get keySpec
	keySpec, err := proto.DecodeKeySpec(req.KeySpec)
	if err != nil {
		return nil, err
	}

	// get hash name and validate hash
	hashName, err := proto.HashAlgorithmFromKeySpec(keySpec)
	if err != nil {
		return nil, err
	}
	if hashName != req.Hash {
		return nil, fmt.Errorf("keySpec hash: %v mismatch request hash: %v", hashName, req.Hash)
	}

	// get signing alg
	signAlg := keySpecToAlg(req.KeySpec)
	if signAlg == "" {
		return nil, errors.New("unrecognized key spec: " + string(req.KeySpec))
	}

	// compute hash for the payload
	hashed, err := computeHash(keySpec.SignatureAlgorithm().Hash(), req.Payload)
	if err != nil {
		return nil, err
	}

	// sign
	sig, err := kv.Sign(ctx, signAlg, hashed)
	if err != nil {
		return nil, err
	}

	// get certificate chain
	rawCertChain, err := getCertificateChain(ctx, kv, req.PluginConfig)
	if err != nil {
		return nil, err
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

func getCertificateChain(ctx context.Context, kv keyvault.KeyVaultCertificate, pluginConfig map[string]string) ([][]byte, error) {
	var (
		certs []*x509.Certificate
		err   error
	)

	if v := pluginConfig[certutils.CertSecretKey]; v == "true" {
		// get the certificate chain by GetSecret (get secret permission)
		certs, err = kv.CertificateChain(ctx)
		if err != nil {
			return nil, err
		}
	} else {
		// get the leaf cert by GetCertificate (get certificate permission)
		cert, err := kv.Certificate(ctx)
		if err != nil {
			return nil, err
		}
		certs = append(certs, cert)
	}

	// validate and build certificate chain from original certs fetched from AKV.
	validCertChain, err := certutils.ValidateCertificateChain(certs)
	if err != nil {
		// if the certs are not enough to build the chain,
		// try to build cert chain with ca_certs of pluginConfig
		certBundlePath, ok := pluginConfig[certutils.CertBundleKey]
		if !ok {
			return nil, fmt.Errorf("failed to build a certificate chain using certificates fetched from AKV because %w. Try again with a certificate bundle file (including intermediate and root certificates) in PEM format through pluginConfig with `ca_certs` as key name and file path as value, or if your Azure KeyVault certificate containing full certificate chain, please set %s=true through pluginConfig to enable accessing certificate chain with GetSecret permission", err, certutils.CertSecretKey)
		}
		if validCertChain, err = certutils.MergeCertificateChain(certBundlePath, certs); err != nil {
			return nil, fmt.Errorf("failed to build a certificate chain with certificate bundle because %w", err)
		}
	}

	// build raw cert chain
	rawCertChain := make([][]byte, 0, len(validCertChain))
	for _, cert := range validCertChain {
		rawCertChain = append(rawCertChain, cert.Raw)
	}
	return rawCertChain, nil
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

func keySpecToAlg(k proto.KeySpec) azkeys.JSONWebKeySignatureAlgorithm {
	switch k {
	case proto.KeySpecRSA2048:
		return azkeys.JSONWebKeySignatureAlgorithmPS256
	case proto.KeySpecRSA3072:
		return azkeys.JSONWebKeySignatureAlgorithmPS384
	case proto.KeySpecRSA4096:
		return azkeys.JSONWebKeySignatureAlgorithmPS512
	case proto.KeySpecEC256:
		return azkeys.JSONWebKeySignatureAlgorithmES256
	case proto.KeySpecEC384:
		return azkeys.JSONWebKeySignatureAlgorithmES384
	case proto.KeySpecEC521:
		return azkeys.JSONWebKeySignatureAlgorithmES512
	}
	return ""
}
