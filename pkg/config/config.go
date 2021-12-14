package config

import (
	"github.com/kelseyhightower/envconfig"
	"github.com/pkg/errors"
)

// Config holds configuration from the env variables
type Config struct {
	ClientID     string `envconfig:"AZURE_CLIENT_ID"`
	ClientSecret string `envconfig:"AZURE_CLIENT_SECRET"`
	TenantID     string `envconfig:"AZURE_TENANT_ID"`
}

// ParseConfig parses the configuration from env variables
func ParseConfig() (*Config, error) {
	c := new(Config)
	if err := envconfig.Process("config", c); err != nil {
		return c, err
	}

	// validate parsed config
	if err := validateConfig(c); err != nil {
		return nil, err
	}
	return c, nil
}

// validateConfig validates the configuration
func validateConfig(c *Config) error {
	if c.TenantID == "" {
		return errors.New("tenant ID is required")
	}
	if c.ClientID == "" {
		return errors.New("client ID is required")
	}
	if c.ClientSecret == "" {
		return errors.New("client secret is required")
	}
	return nil
}
