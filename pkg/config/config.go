package config

import (
	"encoding/json"
	"os"
	"path/filepath"
)

const (
	// ApplicationName is the name of the application
	ApplicationName = "notation-akv"
	// FileName is the name of config file
	FileName = "config.json"
)

// File reflects the config file.
type File struct {
	Credentials Credentials `json:"credentials"`
}

// Credentials is the credentials for the AKV
type Credentials struct {
	ClientID     string `json:"clientId"`
	ClientSecret string `json:"clientSecret"`
	TenantID     string `json:"tenantId"`
}

// Load reads the config from file
func Load() (*File, error) {
	fp, err := getFilePath()
	if err != nil {
		return nil, err
	}

	file, err := os.Open(fp)
	if err != nil {
		return nil, err
	}
	defer file.Close()
	var config *File
	if err := json.NewDecoder(file).Decode(&config); err != nil {
		return nil, err
	}
	return config, nil
}

func getFilePath() (string, error) {
	// init home directories
	configDir, err := os.UserConfigDir()
	if err != nil {
		return "", err
	}
	configDir = filepath.Join(configDir, ApplicationName)
	return filepath.Join(configDir, FileName), nil
}
