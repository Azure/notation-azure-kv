package main

import (
	"context"
	"crypto/x509"
	_ "embed"
	"errors"
	"strings"
	"testing"

	"github.com/Azure/azure-sdk-for-go/sdk/keyvault/azkeys"
	"github.com/Azure/notation-azure-kv/internal/crypto"
	"github.com/Azure/notation-azure-kv/internal/keyvault"
)

//go:embed testdata/aesPEMCert.pem
var aesPEMCert []byte

//go:embed testdata/validPEMCert.pem
var validPEMCert []byte

type certificateMock struct {
	cert *x509.Certificate
	err  error
}

func (c *certificateMock) Sign(ctx context.Context, algorithm azkeys.JSONWebKeySignatureAlgorithm, digest []byte) ([]byte, error) {
	panic("not implemented") // TODO: Implement
}

func (c *certificateMock) CertificateChain(ctx context.Context) ([]*x509.Certificate, error) {
	panic("not implemented") // TODO: Implement
}

func (c *certificateMock) Certificate(ctx context.Context) (*x509.Certificate, error) {
	return c.cert, c.err
}

func Test_runDescribeKey(t *testing.T) {
	certs, err := crypto.ParseCertificates(validPEMCert, "application/x-pem-file")
	if err != nil {
		t.Fatalf("got err = %s, want no error", err)
	}
	if len(certs) != 1 {
		t.Fatalf("got len(certs) = %d, want 1", len(certs))
	}
	sha1Certs, err := crypto.ParseCertificates(aesPEMCert, "application/x-pem-file")
	if err != nil {
		t.Fatalf("got err = %s, want no error", err)
	}
	if len(sha1Certs) != 1 {
		t.Fatalf("got len(certs) = %d, want 1", len(certs))
	}
	type args struct {
		ctx   context.Context
		input string
		fun   func(id string) (keyvault.Certificate, error)
	}
	tests := []struct {
		name    string
		args    args
		wantErr bool
	}{
		{
			name: "invalid AES signed Certificate",
			args: args{
				input: `{"contractVersion":"1.0","keyId":"https://notationakvtest.vault.azure.net/keys/notationrsademo/version"}`,
				fun: func(id string) (keyvault.Certificate, error) {
					return &certificateMock{cert: sha1Certs[0]}, nil
				},
			},
			wantErr: true,
		},
		{
			name: "valid describe key",
			args: args{
				input: `{"contractVersion":"1.0","keyId":"https://notationakvtest.vault.azure.net/keys/notationrsademo/version"}`,
				fun: func(id string) (keyvault.Certificate, error) {
					return &certificateMock{cert: certs[0]}, nil
				},
			},
			wantErr: false,
		},
		{
			name: "newCertificateFromID error",
			args: args{
				input: `{"contractVersion":"1.0","keyId":"https://notationakvtest.vault.azure.net/keys/notationrsademo/version"}`,
				fun: func(id string) (keyvault.Certificate, error) {
					return &certificateMock{}, errors.New("error")
				},
			},
			wantErr: true,
		},
		{
			name: "get certificate failed",
			args: args{
				input: `{"contractVersion":"1.0","keyId":"https://notationakvtest.vault.azure.net/keys/notationrsademo/version"}`,
				fun: func(id string) (keyvault.Certificate, error) {
					return &certificateMock{err: errors.New("error")}, nil
				},
			},
			wantErr: true,
		},
		{
			name: "parse input error",
			args: args{
				input: `{"contractVersion":,"keyId":"https://notationakvtest.vault.azure.net/keys/notationrsademo/version"}`,
				fun: func(id string) (keyvault.Certificate, error) {
					return &certificateMock{}, nil
				},
			},
			wantErr: true,
		},
		{
			name: "keyID error",
			args: args{
				input: `{"contractVersion": "1.0","keyId":""}`,
				fun:   keyvault.NewCertificateFromID,
			},
			wantErr: true,
		},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			input := strings.NewReader(tt.args.input)
			newCertificateFromID = tt.args.fun
			_, err := runDescribeKey(tt.args.ctx, input)
			if (err != nil) != tt.wantErr {
				t.Errorf("runDescribeKey() error = %v, wantErr %v", err, tt.wantErr)
				return
			}
		})
	}
}
