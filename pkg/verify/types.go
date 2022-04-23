package verify

import "github.com/notaryproject/notation-go"

// Request is the request to verify a signature.
type Request struct {
	Version       string                 `json:"version"`
	Signature     []byte                 `json:"signature"`
	VerifyOptions notation.VerifyOptions `json:"verifyOptions"`
	KMSProfile    KMSProfileSuite        `json:"kmsProfile"`
}

// KMSProfileSuite is a named kms key profile with key id and type.
type KMSProfileSuite struct {
	Name       string `json:"name"`
	PluginName string `json:"pluginName"`
	ID         string `json:"id"`
}
