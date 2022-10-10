package crypto

import (
	"crypto/x509"
	"encoding/base64"
	"encoding/pem"

	"golang.org/x/crypto/pkcs12"
)

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
			block, rest = pem.Decode(rest)
		}
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
	return certs, err
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
