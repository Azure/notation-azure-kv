package crypto

import (
	"bytes"
	"crypto/x509"
	"encoding/base64"
	"encoding/pem"
	"errors"
	"fmt"
	"os"

	"golang.org/x/crypto/pkcs12"
)

// CertBundleKey defines the key name for the path of a certificate bundle file
// passing through pluginConfig
const CertBundleKey = "ca_certs"

func parsePEM(data []byte) ([]*x509.Certificate, error) {
	var certs []*x509.Certificate
	block, rest := pem.Decode(data)
	for block != nil {
		// skip private key
		if block.Type == "CERTIFICATE" {
			cert, err := x509.ParseCertificate(block.Bytes)
			if err != nil {
				return nil, err
			}
			certs = append(certs, cert)
		}
		block, rest = pem.Decode(rest)
	}
	return certs, nil
}

func parsePKCS12(data []byte) ([]*x509.Certificate, error) {
	pfx, err := base64.StdEncoding.DecodeString(string(data))
	if err != nil {
		return nil, err
	}
	// https://learn.microsoft.com/en-us/azure/key-vault/certificates/faq
	// password won't be saved.
	blocks, err := pkcs12.ToPEM(pfx, "")
	if err != nil {
		return nil, err
	}
	var certs []*x509.Certificate
	for _, block := range blocks {
		// skip private key
		if block.Type == "CERTIFICATE" {
			cert, err := x509.ParseCertificate(block.Bytes)
			if err != nil {
				return nil, err
			}
			certs = append(certs, cert)
		}
	}
	return certs, nil
}

// ParseCertificates parses certificates from either PEM or PKCS12 data.
// It returns an empty list if no certificates are found.
// Parsing will skip private key.
func ParseCertificates(data []byte, contentType string) (certs []*x509.Certificate, err error) {
	if contentType == "application/x-pkcs12" {
		return parsePKCS12(data)
	}
	return parsePEM(data)
}

// MergeCertificateChain is a function that takes in a plugin configuration map
// and a slice of x509 certificate instances, and attempts to merge the
// certificate chain of the original certificates with a certificate bundle
// specified in the plugin configuration.
func MergeCertificateChain(certBundlePath string, originalCerts []*x509.Certificate) ([]*x509.Certificate, error) {
	// read certificate bundle
	certBundleBytes, err := os.ReadFile(certBundlePath)
	if err != nil {
		return nil, fmt.Errorf("cannot open user provided cert bundle file %q with err: %w", certBundlePath, err)
	}
	certBundle, err := parsePEM(certBundleBytes)
	if err != nil {
		return nil, fmt.Errorf("cannot parse user provided cert bundle file %q with err: %w", certBundlePath, err)
	}
	if len(certBundle) == 0 {
		return nil, errors.New("the certificate bundle file is empty or not in PEM format")
	}

	return ValidateCertificateChain(append(originalCerts, certBundle...))
}

// ValidateCertificateChain verifies a certificate chain and returns the valid
// chain coupled with any error that may occur.
func ValidateCertificateChain(certs []*x509.Certificate) ([]*x509.Certificate, error) {
	// generate certificate pools
	rootPool := x509.NewCertPool()
	intermediatePool := x509.NewCertPool()
	for _, cert := range certs {
		if bytes.Equal(cert.RawIssuer, cert.RawSubject) {
			rootPool.AddCert(cert)
		} else {
			intermediatePool.AddCert(cert)
		}
	}

	// verify options
	opts := x509.VerifyOptions{
		Roots:         rootPool,
		Intermediates: intermediatePool,
		KeyUsages:     []x509.ExtKeyUsage{x509.ExtKeyUsageAny},
	}
	// leaf certificate
	leafCert := certs[0]

	// verify and build certificate chain for leaf.
	certChains, err := leafCert.Verify(opts)
	if err != nil {
		return nil, err
	}
	return certChains[0], nil
}
