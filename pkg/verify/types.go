package verify

import "github.com/notaryproject/notation-go-lib"

// VerifyRequest is the request to verify a signature.
type VerifyRequest struct {
	Version       string                 `json:"version"`
	Signature     []byte                 `json:"signature"`
	VerifyOptions notation.VerifyOptions `json:"verifyOptions"`
	Key           KMSKeySuite            `json:"key"`
}

// KMSKeySuite is a named kms key suite with key id and type.
type KMSKeySuite struct {
	Name       string `json:"name"`
	PluginName string `json:"pluginName"`
	ID         string `json:"id"`
}
