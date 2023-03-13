package crypto

import (
	"crypto"
	"errors"
)

// HashPayload computes the digest of the message with the given HashPayload algorithm.
func HashPayload(message []byte, hash crypto.Hash) ([]byte, error) {
	if !hash.Available() {
		return nil, errors.New("unavailable hash function: " + hash.String())
	}
	h := hash.New()
	if _, err := h.Write(message); err != nil {
		return nil, err
	}
	return h.Sum(nil), nil
}
