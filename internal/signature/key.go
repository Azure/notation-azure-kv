package signature

import (
	"context"
	"errors"
	"fmt"

	"github.com/Azure/notation-azure-kv/internal/cloud"
	"github.com/notaryproject/notation-core-go/signature"
	"github.com/notaryproject/notation-go/plugin"
)

func newKey(keyID string, pluginConfig map[string]string) (*cloud.Key, error) {
	client, err := cloud.NewAzureClient()
	if err != nil {
		return nil, err
	}
	if vaultName := pluginConfig["vaultName"]; vaultName != "" {
		keyVersion := pluginConfig["keyVersion"]
		return cloud.NewKey(client, vaultName, keyID, keyVersion)
	}
	return cloud.NewKeyFromID(client, keyID)
}

func Key(ctx context.Context, req *plugin.DescribeKeyRequest) (*plugin.DescribeKeyResponse, error) {
	if req == nil || req.KeyID == "" {
		return nil, plugin.RequestError{
			Code: plugin.ErrorCodeValidation,
			Err:  errors.New("invalid request input"),
		}
	}
	key, err := newKey(req.KeyID, req.PluginConfig)
	if err != nil {
		return nil, plugin.RequestError{
			Code: plugin.ErrorCodeValidation,
			Err:  err,
		}
	}
	cert, err := key.CertificateChain(ctx)
	if err != nil {
		return nil, requestErr(err)
	}
	keySpec, err := signature.ExtractKeySpec(cert[0])
	if err != nil {
		return nil, fmt.Errorf("get key spec err: %w", err)
	}
	return &plugin.DescribeKeyResponse{
		KeyID:   req.KeyID,
		KeySpec: plugin.KeySpecString(keySpec),
	}, nil
}
