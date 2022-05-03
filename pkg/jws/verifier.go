package jws

import (
	"crypto/x509"

	"github.com/notaryproject/notation-go"
	"github.com/notaryproject/notation-go/signature/jws"
)

func GetVerifier(keyID string) (notation.Verifier, error) {
	cert, err := newCert(keyID)
	if err != nil {
		return nil, err
	}

	roots := x509.NewCertPool()
	roots.AddCert(cert)

	verifier := jws.NewVerifier()
	verifier.VerifyOptions.Roots = roots
	return verifier, nil
}
