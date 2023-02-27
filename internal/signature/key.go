package signature

import (
	"context"
	"errors"
	"fmt"
	"regexp"

	"github.com/Azure/notation-azure-kv/internal/cloud"
	"github.com/notaryproject/notation-core-go/signature"
	"github.com/notaryproject/notation-go/plugin/proto"
)

// re is the regular expression for parsing keyID: https://{vaultName}.vault.azure.net/keys/{keyName}/{version}
const re = `^https:\/\/(.*?)\.(.*)\/keys\/(.*)\/(.*)`

func newKeyVault(keyID string) (*cloud.KeyVault, error) {
	vaultName, dnsSuffix, keyName, version, err := parseKeyID(keyID)
	if err != nil {
		return nil, err
	}
	return cloud.NewKeyVault(vaultName, dnsSuffix, keyName, version)
}

func Key(ctx context.Context, req *proto.DescribeKeyRequest) (*proto.DescribeKeyResponse, error) {
	if req == nil || req.KeyID == "" {
		return nil, proto.RequestError{
			Code: proto.ErrorCodeValidation,
			Err:  errors.New("invalid request input"),
		}
	}
	kv, err := newKeyVault(req.KeyID)
	if err != nil {
		return nil, proto.RequestError{
			Code: proto.ErrorCodeValidation,
			Err:  err,
		}
	}
	cert, err := kv.Certificate(ctx)
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

func parseKeyID(keyID string) (vaultName, dnsSuffix, keyName, keyVersion string, err error) {
	match := regexp.MustCompile(re).FindStringSubmatch(keyID)
	if len(match) < 5 {
		err = fmt.Errorf("invalid keyID. Preferred keyID schema is https://{vaultName}.vault.azure.net/keys/{keyName}/{version}")
		return
	}
	vaultName = match[1]
	dnsSuffix = match[2]
	keyName = match[3]
	keyVersion = match[4]
	return
}
