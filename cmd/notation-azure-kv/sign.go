package main

import (
	"encoding/json"
	"os"

	"github.com/Azure/notation-azure-kv/pkg/sign"

	"github.com/pkg/errors"
	"github.com/urfave/cli/v2"
)

var signCommand = &cli.Command{
	Name:      "sign",
	Usage:     "Sign artifacts with keys in Azure Key Vault",
	ArgsUsage: "<reference>",
	Action:    runSign,
}

func runSign(ctx *cli.Context) error {
	// initialize
	args := ctx.Args()
	if args.Len() != 1 {
		return errors.New("missing request")
	}

	// parse request
	var req sign.SignRequest
	if err := json.Unmarshal([]byte(args.Get(0)), &req); err != nil {
		return err
	}

	signer, err := sign.GetSigner(req.KMSProfile)
	if err != nil {
		return err
	}
	sig, err := signer.Sign(ctx.Context, req.Descriptor, req.SignOptions)
	if err != nil {
		return err
	}

	// write response
	os.Stdout.Write(sig)
	return nil
}
