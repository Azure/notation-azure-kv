package main

import (
	"context"
	"encoding/json"
	"fmt"
	"io"

	"github.com/Azure/notation-azure-kv/internal/signature"
	"github.com/notaryproject/notation-go/plugin/proto"
)

func runDescribeKey(ctx context.Context, input io.Reader) (*proto.DescribeKeyResponse, error) {
	var req proto.DescribeKeyRequest
	if err := json.NewDecoder(input).Decode(&req); err != nil {
		return nil, &proto.RequestError{
			Code: proto.ErrorCodeValidation,
			Err:  fmt.Errorf("failed to unmarshal request input: %w", err),
		}
	}

	return signature.Key(ctx, &req)
}
