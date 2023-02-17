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

	// merge certificate chain
	return mergeCertificateChain(originalCerts, certBundle)
}

// mergeCertificateChain is a helper function for MergeCertificateChain function.
// It obtains the originalCerts and a new certBundle and appends the latter
// to the former then calls ValidateCertChain function, returns the result.
func mergeCertificateChain(originalCerts, certBundle []*x509.Certificate) ([]*x509.Certificate, error) {
	return ValidateCertificateChain(append(originalCerts, certBundle...))
}

// ValidateCertificateChain is a function that takes in a slice of x509 certificate
// instances and validates the certificate chain. It first generates two
// empty certificate pools, rootPool and intermediatePool, and then
// iterates through the input `certs` to classify each one as either a "root"
// or "intermediate" certificate and adding it to the appropriate pool.
// It then sets up the options for certificate chain verification and calls
// leafCert.Verify on the first certificate in the input slice (which is
// assumed to be the "leaf certificate"). If the verification is successful,
// it returns the first chain of authenticated certificates, otherwise it
// logs the error and returns nil and an error.
func ValidateCertificateChain(certs []*x509.Certificate) ([]*x509.Certificate, error) {
	// generate certificate pools
	rootPool := x509.NewCertPool()
	intermediatePool := x509.NewCertPool()
	for _, cert := range certs[1:] {
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
		return nil, fmt.Errorf("failed to validate certificate chain. error: %w", err)
	}
	return certChains[0], nil
}
