package main

import (
	"context"
	"encoding/json"
	"fmt"
	"io"

	"github.com/Azure/notation-azure-kv/internal/keyvault"
	"github.com/notaryproject/notation-core-go/signature"
	"github.com/notaryproject/notation-go/plugin/proto"
)

// newCertificateFromID is the function for generating a key vault certificate
// client. It will be overridden by unit test.
var newCertificateFromID = keyvault.NewCertificateFromID

func runDescribeKey(ctx context.Context, input io.Reader) (*proto.DescribeKeyResponse, error) {
	// parse input request
	var req proto.DescribeKeyRequest
	if err := json.NewDecoder(input).Decode(&req); err != nil {
		return nil, &proto.RequestError{
			Code: proto.ErrorCodeValidation,
			Err:  fmt.Errorf("failed to unmarshal request input: %w", err),
		}
	}

	// get key spec for notation
	keySpec, err := notationKeySpec(ctx, req.KeyID)
	if err != nil {
		return nil, err
	}

	return &proto.DescribeKeyResponse{
		KeyID:   req.KeyID,
		KeySpec: keySpec,
	}, nil
}

func notationKeySpec(ctx context.Context, keyID string) (proto.KeySpec, error) {
	// get the certificate
	certificate, err := newCertificateFromID(keyID)
	if err != nil {
		return "", err
	}
	cert, err := certificate.Certificate(ctx)
	if err != nil {
		return "", err
	}

	// extract key spec from certificate
	keySpec, err := signature.ExtractKeySpec(cert)
	if err != nil {
		return "", err
	}
	return proto.EncodeKeySpec(keySpec)
}
