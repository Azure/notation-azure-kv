package sign

import (
	"github.com/notaryproject/notation-go-lib"
)

// Request is the request to sign artifacts
type Request struct {
	Version     string               `json:"version"`
	Descriptor  notation.Descriptor  `json:"descriptor"`
	SignOptions notation.SignOptions `json:"signOptions"`
	KMSProfile  KMSProfileSuite      `json:"kmsProfile"`
}

// KMSProfileSuite is a named kms key profile with key id and type.
type KMSProfileSuite struct {
	Name       string `json:"name"`
	PluginName string `json:"pluginName"`
	ID         string `json:"id"`
}
