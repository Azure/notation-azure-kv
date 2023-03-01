package main

import (
	"encoding/json"
	"os"

	"github.com/Azure/notation-azure-kv/internal/version"
	"github.com/notaryproject/notation-go/plugin/proto"
)

var metadata []byte

func init() {
	var err error
	metadata, err = json.Marshal(proto.GetMetadataResponse{
		Name:                      "azure-kv",
		Description:               "Sign artifacts with keys in Azure Key Vault",
		Version:                   version.GetVersion(),
		URL:                       "https://github.com/Azure/notation-azure-kv",
		SupportedContractVersions: []string{proto.ContractVersion},
		Capabilities:              []proto.Capability{proto.CapabilitySignatureGenerator},
	})
	if err != nil {
		panic(err)
	}
}

func runGetMetadata() {
	// write response
	os.Stdout.Write(metadata)
}
