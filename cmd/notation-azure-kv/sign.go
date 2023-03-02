package main

import (
	"context"
	"encoding/json"
	"fmt"
	"os"

	"github.com/Azure/notation-azure-kv/internal/signature"
	"github.com/notaryproject/notation-go/plugin/proto"
)

func runSign(ctx context.Context) error {
	var req proto.GenerateSignatureRequest
	err := json.NewDecoder(os.Stdin).Decode(&req)
	if err != nil {
		return &proto.RequestError{
			Code: proto.ErrorCodeValidation,
			Err:  fmt.Errorf("failed to unmarshal request input: %w", err),
		}
	}

	resp, err := signature.Sign(ctx, &req)
	if err != nil {
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
