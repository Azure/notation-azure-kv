package main

import (
	"context"
	"crypto"
	"crypto/x509"
	"encoding/json"
	"errors"
	"fmt"
	"io"

	"github.com/Azure/azure-sdk-for-go/sdk/keyvault/azkeys"
	certutils "github.com/Azure/notation-azure-kv/internal/crypto"
	"github.com/Azure/notation-azure-kv/internal/keyvault"
	"github.com/notaryproject/notation-go/plugin/proto"
)

func runSign(ctx context.Context, input io.Reader) (*proto.GenerateSignatureResponse, error) {
	var req proto.GenerateSignatureRequest
	if err := json.NewDecoder(input).Decode(&req); err != nil {
		return nil, &proto.RequestError{
			Code: proto.ErrorCodeValidation,
			Err:  fmt.Errorf("failed to unmarshal request input: %w", err),
		}
	}

	// create AKV certificate client
	certificate, err := newCertificateFromID(req.KeyID)
	if err != nil {
		return nil, err
	}

	// sign the payload
	sig, signatureAlgorithm, err := sign(ctx, certificate, req.Payload, req.KeySpec)
	if err != nil {
		return nil, err
	}

	// get certificate chain
	rawCertChain, err := getCertificateChain(ctx, certificate, req.PluginConfig)
	if err != nil {
		return nil, err
	}

	return &proto.GenerateSignatureResponse{
		KeyID:            req.KeyID,
		Signature:        sig,
		SigningAlgorithm: string(signatureAlgorithm),
		CertificateChain: rawCertChain,
	}, nil
}

// sign generates the signature and SigningAlgorithm for the given payload
// using the specified key and hash algorithm.
func sign(ctx context.Context, certificate keyvault.Certificate, payload []byte, encodedKeySpec proto.KeySpec) ([]byte, proto.SignatureAlgorithm, error) {
	// get keySpec
	keySpec, err := proto.DecodeKeySpec(encodedKeySpec)
	if err != nil {
		return nil, "", err
	}

	// hash the payload
	digest, err := hashPayload(payload, keySpec.SignatureAlgorithm().Hash())
	if err != nil {
		return nil, "", err
	}

	// get signing algorithm
	signAlgorithm := keySpecToAlg(encodedKeySpec)
	if signAlgorithm == "" {
		return nil, "", fmt.Errorf("unrecognized key spec: %v", encodedKeySpec)
	}

	// sign the digest
	sig, err := certificate.Sign(ctx, digest, signAlgorithm)
	if err != nil {
		return nil, "", err
	}

	signatureAlgorithmString, err := proto.EncodeSigningAlgorithm(keySpec.SignatureAlgorithm())
	if err != nil {
		return nil, "", err
	}
	return sig, signatureAlgorithmString, nil
}

func getCertificateChain(ctx context.Context, certificate keyvault.Certificate, pluginConfig map[string]string) ([][]byte, error) {
	var (
		certs []*x509.Certificate
		err   error
	)

	if v := pluginConfig[certutils.CertSecretKey]; v == "true" {
		// get the certificate chain by GetSecret (get secret permission)
		certs, err = certificate.CertificateChain(ctx)
		if err != nil {
			return nil, err
		}
	} else {
		// get the leaf cert by GetCertificate (get certificate permission)
		cert, err := certificate.Certificate(ctx)
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

// hashPayload computes the digest of the message with the given hashPayload algorithm.
func hashPayload(message []byte, hash crypto.Hash) ([]byte, error) {
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
