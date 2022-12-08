package main

import (
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"os"

	"github.com/Azure/notation-azure-kv/internal/signature"
	"github.com/notaryproject/notation-go/plugin/proto"

	"github.com/urfave/cli/v2"
)

var describeKeyCommand = &cli.Command{
	Name:   string(proto.CommandDescribeKey),
	Usage:  "Azure key description",
	Action: runDescribeKey,
	Flags: []cli.Flag{
		&cli.StringFlag{
			Name:      "file",
			Usage:     "request json file",
			TakesFile: true,
			Hidden:    true,
		},
	},
}

func runDescribeKey(ctx *cli.Context) error {
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
	var req proto.DescribeKeyRequest
	err := json.NewDecoder(r).Decode(&req)
	if err != nil {
		return proto.RequestError{
			Code: proto.ErrorCodeValidation,
			Err:  fmt.Errorf("failed to unmarshal request input: %w", err),
		}
	}

	resp, err := signature.Key(ctx.Context, &req)
	if err != nil {
		var rerr proto.RequestError
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
