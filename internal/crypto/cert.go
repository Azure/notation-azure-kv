package crypto

import (
	"bytes"
	"crypto/x509"
	"encoding/base64"
	"encoding/pem"
	"errors"
	"os"

	"golang.org/x/crypto/pkcs12"
)

const certBundleKey = "certbundle"

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

// CompleteCertificateChain returns the complete certificate chain by appending the parent certificate from the certificate bundle to the original certificate chain.
// If the certificate bundle path is not set in the pluginConfig, it returns the original certificate chain.
// If an error occurs while reading the certificate bundle or parsing its PEM, it returns a nil slice and a non-nil error indicating the failure reason.
func CompleteCertificateChain(pluginConfig map[string]string, originalCerts []*x509.Certificate) ([]*x509.Certificate, error) {
	certBundlePath, ok := pluginConfig[certBundleKey]
	// if certbundle is not set, return the original certs
	if !ok {
		return originalCerts, nil
	}

	certBundleBytes, err := os.ReadFile(certBundlePath)
	if err != nil {
		return nil, err
	}

	certBundle, err := parsePEM(certBundleBytes)
	if err != nil {
		return nil, err
	}
	return completeCertChain(originalCerts, certBundle)
}

// completeCertChain appends the parent certificate from the certificate bundle to the original certificate chain.
// It returns the complete certificate chain along with a nil error if successful, otherwise it returns a nil slice and a non-nil error indicating the failure reason.
func completeCertChain(originalCerts, certBundle []*x509.Certificate) ([]*x509.Certificate, error) {
	if len(originalCerts) == 0 {
		return nil, errors.New("the original certificate chain is empty")
	}

	if len(certBundle) == 0 {
		return nil, errors.New("the certificate bundle is empty")
	}

	oldestCert := originalCerts[len(originalCerts)-1]
	parentCertIndex := -1
	for i, cert := range certBundle {
		if bytes.Equal(oldestCert.RawIssuer, cert.RawSubject) {
			parentCertIndex = i
			break
		}
	}

	if parentCertIndex == -1 {
		return nil, errors.New("cannot find the parent for the original certificate chain from the certificate bundle")
	}
	return append(originalCerts, certBundle[parentCertIndex:]...), nil
}
