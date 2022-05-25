package main

import (
	"encoding/json"
	"os"

	"github.com/Azure/notation-azure-kv/internal/version"
	"github.com/notaryproject/notation-go/plugin"

	"github.com/urfave/cli/v2"
)

var metadataCommand = &cli.Command{
	Name:   string(plugin.CommandGetMetadata),
	Usage:  "Get plugin metadata",
	Action: runGetMetadata,
	Flags: []cli.Flag{
		&cli.StringFlag{
			Name:      "file",
			Usage:     "request json file",
			TakesFile: true,
			Hidden:    true,
		},
	},
}

var metadata []byte

func init() {
	var err error
	metadata, err = json.Marshal(plugin.Metadata{
		Name:                      "azure-kv",
		Description:               "Sign artifacts with keys in Azure Key Vault",
		Version:                   version.GetVersion(),
		URL:                       "https://github.com/Azure/notation-azure-kv",
		SupportedContractVersions: []string{plugin.ContractVersion},
		Capabilities:              []plugin.Capability{plugin.CapabilitySignatureGenerator},
	})
	if err != nil {
		panic(err)
	}
}

func runGetMetadata(ctx *cli.Context) error {
	// write response
	os.Stdout.Write(metadata)
	return nil
}
