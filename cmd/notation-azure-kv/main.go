package main

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"os"

	"github.com/Azure/notation-azure-kv/internal/version"
	"github.com/notaryproject/notation-go/plugin/proto"
)

func main() {
	if len(os.Args) < 2 {
		help()
		return
	}
	ctx := context.Background()
	var err error
	switch proto.Command(os.Args[1]) {
	case proto.CommandGetMetadata:
		err = runGetMetadata()
	case proto.CommandDescribeKey:
		err = runDescribeKey(ctx)
	case proto.CommandGenerateSignature:
		err = runSign(ctx)
	default:
		err = fmt.Errorf("invalid command: %s", os.Args[1])
	}

	if err != nil {
		var reer proto.RequestError
		if !errors.As(err, &reer) {
			err = proto.RequestError{
				Code: proto.ErrorCodeGeneric,
				Err:  err,
			}
		}
		data, _ := json.Marshal(err)
		os.Stderr.Write(data)
		os.Exit(1)
	}
}

func help() {
	fmt.Printf(`notation-azure-kv - Notation - Notary V2 Azure KV plugin

Usage:
  notation-azure-kv <command>

Version:
  %s

Commands:
  describe-key         Azure key description
  generate-signature   Sign artifacts with keys in Azure Key Vault
  get-plugin-metadata  Get plugin metadata

Documentation:
  https://github.com/notaryproject/notaryproject/blob/v1.0.0-rc.2/specs/plugin-extensibility.md#plugin-contract
`, version.GetVersion())
}
