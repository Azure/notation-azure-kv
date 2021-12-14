package main

import (
	"encoding/json"
	"os"

	"github.com/Azure/notation-azure-kv/pkg/verify"

	"github.com/pkg/errors"
	"github.com/urfave/cli/v2"
)

var verifyCommand = &cli.Command{
	Name:      "verify",
	Usage:     "Verify OCI Artifacts",
	ArgsUsage: "<reference>",
	Action:    runVerify,
}

func runVerify(ctx *cli.Context) error {
	// initialize
	args := ctx.Args()
	if args.Len() != 1 {
		return errors.New("missing request")
	}

	// parse request
	var req verify.VerifyRequest
	if err := json.Unmarshal([]byte(args.Get(0)), &req); err != nil {
		return err
	}

	verifier, err := verify.GetVerifier(req.KMSProfile)
	if err != nil {
		return err
	}
	desc, err := verifier.Verify(ctx.Context, req.Signature, req.VerifyOptions)
	if err != nil {
		return err
	}

	var out []byte
	if out, err = json.Marshal(desc); err != nil {
		return err
	}

	// write response
	os.Stdout.Write(out)
	return nil
}
