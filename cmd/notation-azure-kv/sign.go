package main

import (
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"os"

	"github.com/Azure/notation-azure-kv/pkg/jws"
	"github.com/notaryproject/notation-go/plugin"

	"github.com/urfave/cli/v2"
)

var signCommand = &cli.Command{
	Name:   string(plugin.CommandGenerateSignature),
	Usage:  "Sign artifacts with keys in Azure Key Vault",
	Action: runSign,
}

func runSign(ctx *cli.Context) error {
	var r io.Reader
	if f := ctx.String("file"); f != "" {
		var err error
		r, err = os.Open(f)
		if err != nil {
			return err
		}
	} else {
		r = os.Stdin
	}
	var req plugin.GenerateSignatureRequest
	err := json.NewDecoder(r).Decode(&req)
	if err != nil {
		return plugin.RequestError{
			Code: plugin.ErrorCodeValidation,
			Err:  fmt.Errorf("failed to unmarshal request input: %w", err),
		}
	}

	resp, err := jws.Sign(ctx.Context, &req)
	if err != nil {
		var rerr plugin.RequestError
		if errors.As(err, &rerr) {
			return rerr
		}
		return fmt.Errorf("failed to sign payload: %w", err)
	}

	jsonResp, err := json.Marshal(resp)
	if err != nil {
		return err
	}

	// write response
	os.Stdout.Write(jsonResp)
	return nil
}
