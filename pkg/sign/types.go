package sign

import (
	"github.com/notaryproject/notation-go-lib"
)

// SignRequest is the request to sign artifacts
type SignRequest struct {
	Version     string               `json:"version"`
	Descriptor  notation.Descriptor  `json:"descriptor"`
	SignOptions notation.SignOptions `json:"signOptions"`
	Key         KMSKeySuite          `json:"key"`
}

// KMSKeySuite is a named kms key suite with key id and type.
type KMSKeySuite struct {
	Name       string `json:"name"`
	PluginName string `json:"pluginName"`
	ID         string `json:"id"`
}
