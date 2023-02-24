package signature

import "testing"

func Test_parseKeyID(t *testing.T) {
	type args struct {
		keyID string
	}
	tests := []struct {
		name          string
		args          args
		wantVaultName string
		wantDNSSuffix string
		wantKeyName   string
		wantSha       string
		wantErr       bool
	}{
		{
			name:          "valid keyID",
			args:          args{keyID: "https://akvname.vault.azure.net/keys/keyname/b33b9e97ed0b4569b8cdede2162f4000"},
			wantVaultName: "akvname",
			wantDNSSuffix: "vault.azure.net",
			wantKeyName:   "keyname",
			wantSha:       "b33b9e97ed0b4569b8cdede2162f4000",
			wantErr:       false,
		},
		{
			name:          "valid keyID",
			args:          args{keyID: "https://akvname.vault-test.azure.net/keys/keyname/b33b9e97ed0b4569b8cdede2162f4000"},
			wantVaultName: "akvname",
			wantDNSSuffix: "vault-test.azure.net",
			wantKeyName:   "keyname",
			wantSha:       "b33b9e97ed0b4569b8cdede2162f4000",
			wantErr:       false,
		},
		{
			name:    "invalid keyID",
			args:    args{keyID: "http://akvname.vault.azure.net/keys/keyname/b33b9e97ed0b4569b8cdede2162f4000"},
			wantErr: true,
		},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			gotVaultName, gotDnsSuffix, gotKeyName, gotSha, err := parseKeyID(tt.args.keyID)
			if (err != nil) != tt.wantErr {
				t.Errorf("parseKeyID() error = %v, wantErr %v", err, tt.wantErr)
				return
			}
			if gotVaultName != tt.wantVaultName {
				t.Errorf("parseKeyID() gotVaultName = %v, want %v", gotVaultName, tt.wantVaultName)
			}
			if gotDnsSuffix != tt.wantDNSSuffix {
				t.Errorf("parseKeyID() gotDnsSuffix = %v, want %v", gotVaultName, tt.wantVaultName)
			}
			if gotKeyName != tt.wantKeyName {
				t.Errorf("parseKeyID() gotKeyName = %v, want %v", gotKeyName, tt.wantKeyName)
			}
			if gotSha != tt.wantSha {
				t.Errorf("parseKeyID() gotSha = %v, want %v", gotSha, tt.wantSha)
			}
		})
	}
}
