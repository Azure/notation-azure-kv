package crypto

import (
	"crypto/x509"
	_ "embed"
	"testing"
)

//go:embed testdata/validPEMCert.pem
var validPEMCert []byte

//go:embed testdata/invalidPEMCert.pem
var invalidPEMCert []byte

//go:embed testdata/multipleValidPEMCert.pem
var multipleValidPEMCert []byte

//go:embed testdata/validPKCS12Cert.base64
var validPKCS12Cert []byte

//go:embed testdata/PKCS12CertWithNonexportableKey.base64
var pkcs12CertWithNonexportableKey []byte

//go:embed testdata/validLeafPEMcert.pem
var validLeafPEMcert []byte

func Test_parsePEM(t *testing.T) {
	type args struct {
		data []byte
	}
	tests := []struct {
		name        string
		args        args
		wantCertLen int
		wantErr     bool
	}{
		{
			name:        "valid cert",
			args:        args{data: validPEMCert},
			wantCertLen: 1,
			wantErr:     false,
		},
		{
			name:        "invalid cert",
			args:        args{data: invalidPEMCert},
			wantCertLen: 0,
			wantErr:     true,
		},
		{
			name:        "multiple certs in a PEM file",
			args:        args{data: multipleValidPEMCert},
			wantCertLen: 2,
			wantErr:     false,
		},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			certs, err := parsePEM(tt.args.data)
			if (err != nil) != tt.wantErr {
				t.Errorf("parsePEM() error = %v, wantErr %v", err, tt.wantErr)
				return
			}
			if len(certs) != tt.wantCertLen {
				t.Fatalf("len(certs) = %d, want %d", len(certs), tt.wantCertLen)
			}
		})
	}
}

func Test_parsePKCS12(t *testing.T) {
	type args struct {
		data []byte
	}
	tests := []struct {
		name         string
		args         args
		wantCertsLen int
		wantErr      bool
	}{
		{
			name:         "valid cert with exportable private key",
			args:         args{data: validPKCS12Cert},
			wantCertsLen: 1,
			wantErr:      false,
		},
		{
			name:         "invalid cert with nonexportable private key",
			args:         args{data: pkcs12CertWithNonexportableKey},
			wantCertsLen: 0,
			wantErr:      true,
		},
		{
			name:         "invalid PKCS12 content",
			args:         args{data: invalidPEMCert},
			wantCertsLen: 0,
			wantErr:      true,
		},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			certs, err := parsePKCS12(tt.args.data)
			if (err != nil) != tt.wantErr {
				t.Errorf("parsePKCS12() error = %v, wantErr %v", err, tt.wantErr)
				return
			}
			if len(certs) != tt.wantCertsLen {
				t.Fatalf("len(certs) = %d, want %d", len(certs), tt.wantCertsLen)
			}
		})
	}
}

func TestParseCertificates(t *testing.T) {
	type args struct {
		data        []byte
		contentType string
	}
	tests := []struct {
		name         string
		args         args
		wantCertsLen int
		wantErr      bool
	}{
		{
			name:         "valid PEM cert",
			args:         args{data: validPEMCert, contentType: "application/x-pem-file"},
			wantCertsLen: 1,
			wantErr:      false,
		},
		{
			name:         "valid PFX cert",
			args:         args{data: validPKCS12Cert, contentType: "application/x-pkcs12"},
			wantCertsLen: 1,
			wantErr:      false,
		},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			certs, err := ParseCertificates(tt.args.data, tt.args.contentType)
			if (err != nil) != tt.wantErr {
				t.Errorf("ParseCertificates() error = %v, wantErr %v", err, tt.wantErr)
				return
			}
			if len(certs) != tt.wantCertsLen {
				t.Fatalf("len(certs) = %d, want %d", len(certs), tt.wantCertsLen)
			}
		})
	}
}

func TestValidateCertificateChain(t *testing.T) {
	caCert, err := parsePEM(validPEMCert)
	if err != nil {
		t.Fatalf("got err = %s, want no error", err)
	}

	leafCert, err := parsePEM(validLeafPEMcert)
	if err != nil {
		t.Fatalf("got err = %s, want no error", err)
	}

	certChain := append(leafCert, caCert...)

	type args struct {
		certs []*x509.Certificate
	}
	tests := []struct {
		name         string
		args         args
		wantCertsLen int
		wantErr      bool
	}{
		{
			name:         "valid single ca cert",
			args:         args{certs: caCert},
			wantCertsLen: 1,
			wantErr:      false,
		},
		{
			name:         "valid cert chain",
			args:         args{certs: certChain},
			wantCertsLen: 2,
			wantErr:      false,
		},
		{
			name:         "invalid leaf cert",
			args:         args{certs: leafCert},
			wantCertsLen: 0,
			wantErr:      true,
		},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			certs, err := ValidateCertificateChain(tt.args.certs)
			if (err != nil) != tt.wantErr {
				t.Errorf("ValidateCertificateChain() error = %v, wantErr %v", err, tt.wantErr)
				return
			}

			if len(certs) != tt.wantCertsLen {
				t.Fatalf("len(certs) = %d, want %d", len(certs), tt.wantCertsLen)
			}
		})
	}
}

func TestMergeCertificateChain(t *testing.T) {
	leafCert, err := parsePEM(validLeafPEMcert)
	if err != nil {
		t.Fatalf("got err = %s, want no error", err)
	}
	type args struct {
		certBundlePath string
		originalCerts  []*x509.Certificate
	}
	tests := []struct {
		name         string
		args         args
		wantCertsLen int
		wantErr      bool
	}{
		{
			name:         "valid cert bundle",
			args:         args{certBundlePath: "./testdata/validPEMCert.pem", originalCerts: leafCert},
			wantCertsLen: 2,
			wantErr:      false,
		},
		{
			name:         "read file error",
			args:         args{certBundlePath: "./testdata/invalidFile.pem", originalCerts: leafCert},
			wantCertsLen: 0,
			wantErr:      true,
		},
		{
			name:         "invalid cert bundle",
			args:         args{certBundlePath: "./testdata/invalidPEMCert.pem", originalCerts: leafCert},
			wantCertsLen: 0,
			wantErr:      true,
		},
		{
			name:         "empty cert bundle",
			args:         args{certBundlePath: "./testdata/PKCS12CertWithNonexportableKey.base64", originalCerts: leafCert},
			wantCertsLen: 0,
			wantErr:      true,
		},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			certs, err := MergeCertificateChain(tt.args.certBundlePath, tt.args.originalCerts)
			if (err != nil) != tt.wantErr {
				t.Errorf("MergeCertificateChain() error = %v, wantErr %v", err, tt.wantErr)
				return
			}
			if len(certs) != tt.wantCertsLen {
				t.Fatalf("len(certs) = %d, want %d", len(certs), tt.wantCertsLen)
			}
		})
	}
}
