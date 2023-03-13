package main

import (
	"context"
	"encoding/json"
	"fmt"
	"io"

	"github.com/Azure/notation-azure-kv/internal/signature"
	"github.com/notaryproject/notation-go/plugin/proto"
)

func runSign(ctx context.Context, input io.Reader) (*proto.GenerateSignatureResponse, error) {
	var req proto.GenerateSignatureRequest
	if err := json.NewDecoder(input).Decode(&req); err != nil {
		return nil, &proto.RequestError{
			Code: proto.ErrorCodeValidation,
			Err:  fmt.Errorf("failed to unmarshal request input: %w", err),
		}
	}

	// create AKV certificate client
	certificate, err := newCertificateFromID(req.KeyID)
	if err != nil {
		return nil, err
	}

	// sign the payload
	sig, signatureAlgorithm, err := signature.Sign(ctx, certificate, req.Payload, req.KeySpec)
	if err != nil {
		return nil, err
	}

	// get certificate chain
	rawCertChain, err := signature.GetCertificateChain(ctx, certificate, req.PluginConfig)
	if err != nil {
		return nil, err
	}

	return &proto.GenerateSignatureResponse{
		KeyID:            req.KeyID,
		Signature:        sig,
		SigningAlgorithm: string(signatureAlgorithm),
		CertificateChain: rawCertChain,
	}, nil
}
