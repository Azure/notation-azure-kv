package main

import (
	"reflect"
	"testing"

	"github.com/notaryproject/notation-go/plugin/proto"
)

func Test_runGetMetadata(t *testing.T) {
	metadata := runGetMetadata()
	if metadata.Name != "azure-kv" {
		t.Fatalf("invalid plugin name: %s", metadata.Name)
	}
	if !reflect.DeepEqual(metadata.SupportedContractVersions, []string{proto.ContractVersion}) {
		t.Fatalf("invalid supported contract versions: %v", metadata.SupportedContractVersions)
	}
	if !reflect.DeepEqual(metadata.Capabilities, []proto.Capability{proto.CapabilitySignatureGenerator}) {
		t.Fatalf("invalid capabilities: %v", metadata.Capabilities)
	}
}
