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
	switch os.Args[1] {
	case string(proto.CommandGetMetadata):
		runGetMetadata()
	case string(proto.CommandDescribeKey):
		err = runDescribeKey(ctx)
	case string(proto.CommandGenerateSignature):
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
	fmt.Printf(`NAME:
   notation-azure-kv - Notation - Notary V2 Azure KV plugin

USAGE:
   notation-azure-kv [global options] command [command options] [arguments...]

VERSION:
   %s

COMMANDS:
   get-plugin-metadata  Get plugin metadata
   generate-signature   Sign artifacts with keys in Azure Key Vault
   describe-key         Azure key description
`, version.GetVersion())
}
