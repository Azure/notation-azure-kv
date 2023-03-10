package keyvault

import (
	"context"
	"errors"
	"os"
	"testing"

	"github.com/Azure/azure-sdk-for-go/sdk/keyvault/azcertificates"
	"github.com/Azure/azure-sdk-for-go/sdk/keyvault/azkeys"
	"github.com/Azure/azure-sdk-for-go/sdk/keyvault/azsecrets"
)

func TestNewCertificateFromID(t *testing.T) {
	type args struct {
		id string
	}
	tests := []struct {
		name    string
		args    args
		wantErr bool
	}{
		{
			name:    "valid key identifier",
			args:    args{id: "https://akvname.vault.azure.net/keys/keyname/b33b9e97ed0b4569b8cdede2162f4000"},
			wantErr: false,
		},
		{
			name:    "valid certificate identifier",
			args:    args{id: "https://akvname.vault.azure.net/certificates/keyname/b33b9e97ed0b4569b8cdede2162f4000"},
			wantErr: false,
		},
		{
			name:    "invalid http schema",
			args:    args{id: "http://akvname.vault.azure.net/keys/keyname/b33b9e97ed0b4569b8cdede2162f4000"},
			wantErr: true,
		},
		{
			name:    "invalid identifier name",
			args:    args{id: "https://akvname.vault.azure.net/key/keyname/b33b9e97ed0b4569b8cdede2162f4000"},
			wantErr: true,
		},
		{
			name:    "invalid version",
			args:    args{id: "https://akvname.vault.azure.net/key/keyname/b33b9e97ed0b4569b8cdede2162f4000/v2"},
			wantErr: true,
		},
		{
			name:    "empty url",
			args:    args{id: ""},
			wantErr: true,
		},
		{
			name:    "invalid URL",
			args:    args{id: "htt\\ps://akvname.vault.azure.net/key/keyname/b33b9e97ed0b4569b8cdede2162f4000/v2"},
			wantErr: true,
		},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			_, err := NewCertificateFromID(tt.args.id)
			if (err != nil) != tt.wantErr {
				t.Errorf("NewCertificateFromID() error = %v, wantErr %v", err, tt.wantErr)
				return
			}
		})
	}
}

type keyVault struct {
	resp azkeys.SignResponse
	err  error
}

func (s *keyVault) Sign(ctx context.Context, name string, version string, parameters azkeys.SignParameters, options *azkeys.SignOptions) (azkeys.SignResponse, error) {
	return s.resp, s.err
}

func TestSign(t *testing.T) {
	t.Run("check return error", func(t *testing.T) {
		certificate := certificate{
			keyClient: &keyVault{err: errors.New("error")},
		}
		_, err := certificate.Sign(context.Background(), nil, "")
		if err == nil {
			t.Fatal("should error")
		}
	})

	t.Run("check KID is nil", func(t *testing.T) {
		certificate := certificate{
			keyClient: &keyVault{
				resp: azkeys.SignResponse{
					KeyOperationResult: azkeys.KeyOperationResult{},
				},
			},
		}
		_, err := certificate.Sign(context.Background(), nil, "")
		if err == nil {
			t.Fatal("should error")
		}
	})

	t.Run("check Result is nil", func(t *testing.T) {
		var keyID azkeys.ID = "id"
		certificate := certificate{
			keyClient: &keyVault{
				resp: azkeys.SignResponse{
					KeyOperationResult: azkeys.KeyOperationResult{
						KID: &keyID,
					},
				},
			},
		}
		_, err := certificate.Sign(context.Background(), nil, "")
		if err == nil {
			t.Fatal("should error")
		}
	})

	t.Run("valid return", func(t *testing.T) {
		var keyID azkeys.ID = "id"
		certificate := certificate{
			keyClient: &keyVault{
				resp: azkeys.SignResponse{
					KeyOperationResult: azkeys.KeyOperationResult{
						KID:    &keyID,
						Result: []byte(""),
					},
				},
			},
		}
		_, err := certificate.Sign(context.Background(), nil, "")
		if err != nil {
			t.Fatalf("got error = %s, expect no error", err)
		}
	})
}

type certificateVault struct {
	resp azcertificates.GetCertificateResponse
	err  error
}

func (s *certificateVault) GetCertificate(ctx context.Context, certificateName string, certificateVersion string, options *azcertificates.GetCertificateOptions) (azcertificates.GetCertificateResponse, error) {
	return s.resp, s.err
}

func TestCertificate(t *testing.T) {
	t.Run("check return error", func(t *testing.T) {
		certificate := certificate{
			certificateClient: &certificateVault{err: errors.New("error")},
		}
		_, err := certificate.Certificate(context.Background())
		if err == nil {
			t.Fatal("should error")
		}
	})

	t.Run("check KID", func(t *testing.T) {
		certificate := certificate{
			certificateClient: &certificateVault{resp: azcertificates.GetCertificateResponse{
				CertificateBundle: azcertificates.CertificateBundle{},
			}},
		}
		_, err := certificate.Certificate(context.Background())
		if err == nil {
			t.Fatal("should error")
		}
	})

	t.Run("invalid cert", func(t *testing.T) {
		var keyID = "id"
		certificate := certificate{
			certificateClient: &certificateVault{resp: azcertificates.GetCertificateResponse{
				CertificateBundle: azcertificates.CertificateBundle{
					KID: &keyID,
					CER: []byte{},
				},
			}},
		}
		_, err := certificate.Certificate(context.Background())
		if err == nil {
			t.Fatal("should error")
		}
	})
}

type secretVault struct {
	resp azsecrets.GetSecretResponse
	err  error
}

func (s *secretVault) GetSecret(ctx context.Context, name string, version string, options *azsecrets.GetSecretOptions) (azsecrets.GetSecretResponse, error) {
	return s.resp, s.err
}

func TestCertificateChain(t *testing.T) {
	t.Run("check return error", func(t *testing.T) {
		certificate := certificate{
			secretClient: &secretVault{err: errors.New("error")},
		}
		_, err := certificate.CertificateChain(context.Background())
		if err == nil {
			t.Fatal("should error")
		}
	})

	t.Run("check Value is not nil", func(t *testing.T) {
		certificate := certificate{
			secretClient: &secretVault{resp: azsecrets.GetSecretResponse{
				SecretBundle: azsecrets.SecretBundle{},
			}},
		}
		_, err := certificate.CertificateChain(context.Background())
		if err == nil {
			t.Fatal("should error")
		}
	})

	t.Run("check ContentType is not nil", func(t *testing.T) {
		var value = "value"
		certificate := certificate{
			secretClient: &secretVault{resp: azsecrets.GetSecretResponse{
				SecretBundle: azsecrets.SecretBundle{
					Value: &value,
				},
			}},
		}
		_, err := certificate.CertificateChain(context.Background())
		if err == nil {
			t.Fatal("should error")
		}
	})

	t.Run("invalid certificate chain", func(t *testing.T) {
		var value = "value"
		var contentType = "application/x-pkcs12"
		certificate := certificate{
			secretClient: &secretVault{resp: azsecrets.GetSecretResponse{
				SecretBundle: azsecrets.SecretBundle{
					Value:       &value,
					ContentType: &contentType,
				},
			}},
		}
		_, err := certificate.CertificateChain(context.Background())
		if err == nil {
			t.Fatal("should error")
		}
	})
}

func Test_azureCredential(t *testing.T) {
	t.Run("CLI credential", func(t *testing.T) {
		os.Setenv("AKV_AUTH_METHOD", "AKV_AUTH_FROM_CLI")
		_, err := azureCredential()
		if err != nil {
			t.Fatalf("got error = %s, expect no error", err)
		}
	})

	t.Run("MI credential", func(t *testing.T) {
		os.Setenv("AKV_AUTH_METHOD", "AKV_AUTH_FROM_MI")
		_, err := azureCredential()
		if err != nil {
			t.Fatalf("got error = %s, expect no error", err)
		}
	})

	t.Run("unknown credential", func(t *testing.T) {
		os.Setenv("AKV_AUTH_METHOD", "AKV_AUTH_FROM_UNKNOWN")
		_, err := azureCredential()
		if err == nil {
			t.Fatal("should error")
		}
	})
}

func TestNewCertificate(t *testing.T) {
	t.Run("unknown credential", func(t *testing.T) {
		os.Setenv("AKV_AUTH_METHOD", "AKV_AUTH_FROM_UNKNOWN")
		_, err := NewCertificate("", "", "")
		if err == nil {
			t.Fatal("should error")
		}
	})
}
