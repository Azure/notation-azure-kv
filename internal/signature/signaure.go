package signature

import (
	"context"
	"crypto/x509"
	"fmt"

	"github.com/Azure/notation-azure-kv/internal/crypto"
	"github.com/Azure/notation-azure-kv/internal/keyvault"
	"github.com/notaryproject/notation-go/plugin/proto"
)

// Sign generates the signature and SigningAlgorithm for the given payload
// using the specified key and hash algorithm.
func Sign(ctx context.Context, certificate keyvault.Certificate, payload []byte, encodedKeySpec proto.KeySpec) ([]byte, proto.SignatureAlgorithm, error) {
	// get keySpec
	keySpec, err := proto.DecodeKeySpec(encodedKeySpec)
	if err != nil {
		return nil, "", err
	}

	// hash the payload
	digest, err := crypto.HashPayload(payload, keySpec.SignatureAlgorithm().Hash())
	if err != nil {
		return nil, "", err
	}

	// get signing algorithm
	signAlgorithm := KeySpecToAlg(encodedKeySpec)
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

func GetCertificateChain(ctx context.Context, certificate keyvault.Certificate, pluginConfig map[string]string) ([][]byte, error) {
	var (
		certs []*x509.Certificate
		err   error
	)

	if v := pluginConfig[crypto.CertSecretKey]; v == "true" {
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
	validCertChain, err := crypto.ValidateCertificateChain(certs)
	if err != nil {
		// if the certs are not enough to build the chain,
		// try to build cert chain with ca_certs of pluginConfig
		certBundlePath, ok := pluginConfig[crypto.CertBundleKey]
		if !ok {
			return nil, fmt.Errorf("failed to build a certificate chain using certificates fetched from AKV because %w. Try again with a certificate bundle file (including intermediate and root certificates) in PEM format through pluginConfig with `ca_certs` as key name and file path as value, or if your Azure KeyVault certificate containing full certificate chain, please set %s=true through pluginConfig to enable accessing certificate chain with GetSecret permission", err, crypto.CertSecretKey)
		}
		if validCertChain, err = crypto.MergeCertificateChain(certBundlePath, certs); err != nil {
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
