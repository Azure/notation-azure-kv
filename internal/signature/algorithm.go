package signature

import (
	"github.com/Azure/azure-sdk-for-go/sdk/keyvault/azkeys"
	"github.com/notaryproject/notation-go/plugin/proto"
)

func KeySpecToAlg(k proto.KeySpec) azkeys.JSONWebKeySignatureAlgorithm {
	switch k {
	case proto.KeySpecRSA2048:
		return azkeys.JSONWebKeySignatureAlgorithmPS256
	case proto.KeySpecRSA3072:
		return azkeys.JSONWebKeySignatureAlgorithmPS384
	case proto.KeySpecRSA4096:
		return azkeys.JSONWebKeySignatureAlgorithmPS512
	case proto.KeySpecEC256:
		return azkeys.JSONWebKeySignatureAlgorithmES256
	case proto.KeySpecEC384:
		return azkeys.JSONWebKeySignatureAlgorithmES384
	case proto.KeySpecEC521:
		return azkeys.JSONWebKeySignatureAlgorithmES512
	}
	return ""
}
