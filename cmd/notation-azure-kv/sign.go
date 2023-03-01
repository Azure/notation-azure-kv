package main

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"os"

	"github.com/Azure/notation-azure-kv/internal/signature"
	"github.com/notaryproject/notation-go/plugin/proto"
)

func runSign(ctx context.Context) error {
	r := os.Stdin
	var req proto.GenerateSignatureRequest
	err := json.NewDecoder(r).Decode(&req)
	if err != nil {
		return proto.RequestError{
			Code: proto.ErrorCodeValidation,
			Err:  fmt.Errorf("failed to unmarshal request input: %w", err),
		}
	}

	resp, err := signature.Sign(ctx, &req)
	if err != nil {
		var rerr proto.RequestError
		if errors.As(err, &rerr) {
			return rerr
		}
		return err
	}

	jsonResp, err := json.Marshal(resp)
	if err != nil {
		return err
	}

	// write response
	os.Stdout.Write(jsonResp)
	return nil
}
