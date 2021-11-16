package main

import (
	"os"

	"github.com/Azure/notation-akv/internal/version"

	"github.com/urfave/cli/v2"
)

func main() {
	app := &cli.App{
		Name:  "notation-akv",
		Usage: "Notation - Notary V2 AKV plugin",
		// TODO(aramase) add version package
		Version: version.GetVersion(),
		Commands: []*cli.Command{
			signCommand,
			verifyCommand,
		},
	}
	if err := app.Run(os.Args); err != nil {
		os.Stderr.WriteString(err.Error())
	}
}
