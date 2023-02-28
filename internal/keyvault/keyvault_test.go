package keyvault

import (
	"testing"
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
