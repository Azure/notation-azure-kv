package main

import (
	"encoding/json"
	"os"

	"github.com/Azure/notation-azure-kv/internal/version"

	"github.com/notaryproject/notation-go/plugin"
	"github.com/urfave/cli/v2"
)

func main() {
	app := &cli.App{
		Name:  "notation-azure-kv",
		Usage: "Notation - Notary V2 Azure KV plugin",
		// TODO(aramase) add version package
		Version: version.GetVersion(),
		Commands: []*cli.Command{
			metadataCommand,
			signCommand,
			describeKeyCommand,
		},
	}
	if err := app.Run(os.Args); err != nil {
		if _, ok := err.(*plugin.RequestError); !ok {
			err = plugin.RequestError{
				Code: plugin.ErrorCodeGeneric,
				Err:  err,
			}
		}
		data, _ := json.Marshal(err)
		os.Stderr.Write(data)
		os.Exit(1)
	}
}
