package crypto

import (
	"bytes"
	"crypto/x509"
	"encoding/base64"
	"encoding/pem"
	"errors"
	"fmt"
	"os"
	"path/filepath"
	"regexp"
	"strings"

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

// MergeCertificateChain returns the complete certificate chain by appending the parent certificate from the certificate bundle to the original certificate chain.
// If the certificate bundle path is not set in the pluginConfig, it returns the original certificate chain.
// If an error occurs while reading the certificate bundle or parsing its PEM, it returns a nil slice and a non-nil error indicating the failure reason.
func MergeCertificateChain(pluginConfig map[string]string, originalCerts []*x509.Certificate) ([]*x509.Certificate, error) {
	// get cert bundle path
	if pluginConfig == nil {
		return originalCerts, nil
	}
	certBundlePath, ok := pluginConfig[certBundleKey]
	if !ok {
		return originalCerts, nil
	}

	// validate certificate bundle path
	certBundlePath = strings.Trim(certBundlePath, " ")
	basename := filepath.Base(certBundlePath)
	if !isValidFileName(basename) {
		return nil, fmt.Errorf("the filename of certificate bundle path is not cross-platform compatible. filename: %s", basename)
	}

	// read certificate bundle
	certBundleBytes, err := os.ReadFile(certBundlePath)
	if err != nil {
		return nil, fmt.Errorf("cannot open user provided cert bundle file %q with err: %w", certBundlePath, err)
	}
	certBundle, err := parsePEM(certBundleBytes)
	if err != nil {
		return nil, fmt.Errorf("cannot parse user provided cert bundle file %q with err: %w", certBundlePath, err)
	}
	// merge certificate chain
	return mergeCertChain(originalCerts, certBundle)
}

// mergeCertChain appends the parent certificate from the certificate bundle to the original certificate chain.
// It returns the complete certificate chain along with a nil error if successful, otherwise it returns a nil slice and a non-nil error indicating the failure reason.
func mergeCertChain(originalCerts, certBundle []*x509.Certificate) ([]*x509.Certificate, error) {
	if len(originalCerts) == 0 {
		return nil, errors.New("the certificate chain accessed from Azure Key Vault is empty")
	}
	if len(certBundle) == 0 {
		return nil, errors.New("the certificate bundle file is empty or in a unsupported format")
	}
	// leaf certificate
	leafCert := originalCerts[0]

	// generate certificate pools
	rootPool := x509.NewCertPool()
	intermediatePool := x509.NewCertPool()

	// verify options
	opts := x509.VerifyOptions{
		Roots:         rootPool,
		Intermediates: intermediatePool,
		KeyUsages:     []x509.ExtKeyUsage{x509.ExtKeyUsageAny},
	}

	// try to validate the certificate chain by using the original certificates
	// accessed from AKV.
	for _, cert := range originalCerts[1:] {
		if bytes.Equal(cert.RawIssuer, cert.RawSubject) {
			rootPool.AddCert(cert)
		} else {
			intermediatePool.AddCert(cert)
		}
	}
	// verify and build certificate chain for leaf CA.
	// skip the error because it will try to use cert bundle to fix it.
	if certChains, err := leafCert.Verify(opts); err == nil {
		return certChains[0], nil
	}

	// if the original certificates are not enough for generating the chain,
	// try to use the certificate bundle.
	for _, cert := range certBundle {
		if bytes.Equal(cert.RawIssuer, cert.RawSubject) {
			rootPool.AddCert(cert)
		} else {
			intermediatePool.AddCert(cert)
		}
	}
	certChains, err := leafCert.Verify(opts)
	if err != nil {
		return nil, fmt.Errorf("cannot merge the certificate chain by the certificate bundle. error: %w", err)
	}
	return certChains[0], nil
}

// isValidFileName checks if a file name is cross-platform compatible
func isValidFileName(fileName string) bool {
	return regexp.MustCompile(`^[a-zA-Z0-9_.-]+$`).MatchString(fileName)
}
