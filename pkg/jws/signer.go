package jws

import (
	"encoding/base64"
	"errors"
	"fmt"
	"net/http"

	"github.com/Azure/azure-sdk-for-go/services/keyvault/v7.1/keyvault"
	"github.com/Azure/go-autorest/autorest/azure"
	jwtazure "github.com/AzureCR/go-jwt-azure"

	"github.com/notaryproject/notation-go/plugin"
	"github.com/notaryproject/notation-go/signature/jws"
)

func Sign(req *plugin.GenerateSignatureRequest) (*plugin.GenerateSignatureResponse, error) {
	if req == nil || req.KeyID == "" {
		return nil, plugin.RequestError{
			Code: plugin.ErrorCodeValidation,
			Err:  errors.New("invalid request input"),
		}
	}
	key, err := newKey(req.KeyID)
	if err != nil {
		return nil, err
	}
	cert, err := key.Certificate()
	if err != nil {
		return nil, requestErr(err)
	}

	method, err := jws.SigningMethodFromKey(cert.PublicKey)
	if err != nil {
		return nil, fmt.Errorf("unrecognized signing method: %w", err)
	}

	alg := keyvault.JSONWebKeySignatureAlgorithm(method.Alg())
	if _, ok := jwtazure.SigningMethods[alg]; !ok {
		return nil, fmt.Errorf("unrecognized azure signing method: %v", alg)
	}

	payload, err := base64.RawStdEncoding.DecodeString(req.Payload)
	if err != nil {
		return nil, err
	}
	sig, err := key.Sign(alg, payload)
	if err != nil {
		return nil, requestErr(err)
	}
	return &plugin.GenerateSignatureResponse{
		KeyID:            req.KeyID,
		Signature:        base64.RawStdEncoding.EncodeToString(sig),
		SigningAlgorithm: string(alg),
		CertificateChain: []string{
			base64.RawStdEncoding.EncodeToString(cert.Raw),
		},
	}, nil
}

func requestErr(err error) plugin.RequestError {
	var code plugin.ErrorCode
	var aerr *azure.RequestError
	if errors.As(err, &aerr) {
		switch aerr.StatusCode {
		case http.StatusUnauthorized:
			code = plugin.ErrorCodeAccessDenied
		case http.StatusRequestTimeout:
			code = plugin.ErrorCodeTimeout
		case http.StatusTooManyRequests:
			code = plugin.ErrorCodeThrottled
		default:
			code = plugin.ErrorCodeGeneric
		}
	}
	return plugin.RequestError{
		Code: code,
		Err:  err,
	}
}
