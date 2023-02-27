package main

import (
	"encoding/json"
	"errors"
	"net/http"
	"os"

	"github.com/Azure/azure-sdk-for-go/sdk/azcore"
	"github.com/Azure/notation-azure-kv/internal/version"
	"github.com/notaryproject/notation-go/plugin/proto"
	"github.com/urfave/cli/v2"
)

func main() {
	app := &cli.App{
		Name:    "notation-azure-kv",
		Usage:   "Notation - Notary V2 Azure KV plugin",
		Version: version.GetVersion(),
		Commands: []*cli.Command{
			metadataCommand,
			signCommand,
			describeKeyCommand,
		},
	}
	if err := app.Run(os.Args); err != nil {
		data, _ := json.Marshal(wrapErr(err))
		os.Stderr.Write(data)
		os.Exit(1)
	}
}

func wrapErr(err error) proto.RequestError {
	// default error code
	code := proto.ErrorCodeGeneric

	// wrap Azure response
	var aerr *azcore.ResponseError
	if errors.As(err, &aerr) {
		switch aerr.StatusCode {
		case http.StatusUnauthorized:
			code = proto.ErrorCodeAccessDenied
		case http.StatusRequestTimeout:
			code = proto.ErrorCodeTimeout
		case http.StatusTooManyRequests:
			code = proto.ErrorCodeThrottled
		}
	}

	return proto.RequestError{
		Code: code,
		Err:  err,
	}
}
