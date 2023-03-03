package main

import (
	"github.com/Azure/notation-azure-kv/internal/version"
	"github.com/notaryproject/notation-go/plugin/proto"
)

func runGetMetadata() *proto.GetMetadataResponse {
	return &proto.GetMetadataResponse{
		Name:                      "azure-kv",
		Description:               "Sign artifacts with keys in Azure Key Vault",
		Version:                   version.GetVersion(),
		URL:                       "https://github.com/Azure/notation-azure-kv",
		SupportedContractVersions: []string{proto.ContractVersion},
		Capabilities:              []proto.Capability{proto.CapabilitySignatureGenerator},
	}
}
