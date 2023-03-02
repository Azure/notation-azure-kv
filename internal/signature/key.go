package signature

import (
	"context"
	"errors"
	"fmt"

	"github.com/Azure/notation-azure-kv/internal/keyvault"
	"github.com/notaryproject/notation-core-go/signature"
	"github.com/notaryproject/notation-go/plugin/proto"
)

func Key(ctx context.Context, req *proto.DescribeKeyRequest) (*proto.DescribeKeyResponse, error) {
	if req == nil || req.KeyID == "" {
		return nil, proto.RequestError{
			Code: proto.ErrorCodeValidation,
			Err:  errors.New("invalid request input"),
		}
	}
	kv, err := keyvault.NewCertificateFromID(req.KeyID)
	if err != nil {
		return nil, proto.RequestError{
			Code: proto.ErrorCodeValidation,
			Err:  err,
		}
	}
	cert, err := kv.CertificateChain(ctx)
	if err != nil {
		return nil, err
	}
	keySpec, err := signature.ExtractKeySpec(cert)
	if err != nil {
		return nil, fmt.Errorf("get key spec err: %w", err)
	}
	notationKeySpec, err := proto.EncodeKeySpec(keySpec)
	if err != nil {
		return nil, fmt.Errorf("encode key spec err: %w", err)
	}
	return &proto.DescribeKeyResponse{
		KeyID:   req.KeyID,
		KeySpec: notationKeySpec,
	}, nil
}
