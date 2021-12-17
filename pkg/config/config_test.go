package config

import (
	"os"
	"testing"
)

func TestParseConfig(t *testing.T) {
	os.Setenv("AZURE_CLIENT_ID", "client_id")
	os.Setenv("AZURE_CLIENT_SECRET", "client_secret")
	os.Setenv("AZURE_TENANT_ID", "tenant_id")
	defer os.Clearenv()

	c, err := ParseConfig()
	if err != nil {
		t.Errorf("ParseConfig() = %v, want nil", err)
	}

	if c.ClientID != "client_id" {
		t.Errorf("ParseConfig() = %v, want client_id", c.ClientID)
	}
	if c.ClientSecret != "client_secret" {
		t.Errorf("ParseConfig() = %v, want client_secret", c.ClientSecret)
	}
	if c.TenantID != "tenant_id" {
		t.Errorf("ParseConfig() = %v, want tenant_id", c.TenantID)
	}
}
