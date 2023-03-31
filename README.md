# notation-azure-kv

[![codecov](https://codecov.io/gh/Azure/notation-azure-kv/branch/main/graph/badge.svg)](https://codecov.io/gh/Azure/notation-azure-kv)

Azure Provider for the [Notation CLI](https://github.com/notaryproject/notation)

The `notation-azure-kv` plugin allows you to sign the Notation-generated payload with a certificate in Azure Key Vault (AKV). The certificate and private key are stored in AKV and the plugin will request signing and obtain the leaf certificate from AKV. 

The plugin supports authentication by [Azure CLI](https://learn.microsoft.com/cli/azure/authenticate-azure-cli) or [Managed Identity](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview). Azure CLI authenticate is used by default. To enable `Managed Identity` authentication, set the `AKV_AUTH_METHOD` environment variable to `AKV_AUTH_FROM_MI`.

## Installation the AKV plugin
Before you begin, make sure the latest version of the [Notation CLI has been installed](https://notaryproject.dev/docs/installation/cli/). 

1. Navigate to the [Releases](https://github.com/Azure/notation-azure-kv/releases) page and select the latest release of `notation-azure-kv`. Under the *Assets* section, select the `notation-azure-kv` binary for your platform.
2. Validate the checksum using the values in `checksums.txt` and then install the plugin.

   For Linux Bash:
   ```sh
   version=0.6.0

   # validate checksum
   cat checksums.txt | grep notation-azure-kv_${version}_linux_amd64.tar.gz | sha256sum -c

   # install the plugin
   mkdir -p "$HOME/.config/notation/plugins/azure-kv"
   tar zxf notation-azure-kv_${version}_linux_amd64.tar.gz -C "$HOME/.config/notation/plugins/azure-kv" notation-azure-kv
   ```
   For macOS Zsh:
   ```sh
   version=0.6.0

   # validate checksum
   cat checksums.txt | grep notation-azure-kv_${version}_darwin_amd64.tar.gz | shasum -a 256 -c

   # install the plugin
   mkdir -p "$HOME/Library/Application Support/notation/plugins/azure-kv"
   tar zxf notation-azure-kv_${version}_darwin_amd64.tar.gz -C "$HOME/Library/Application Support/notation/plugins/azure-kv" notation-azure-kv
   ```
   For Windows Powershell:
   ```powershell
   $version = "0.6.0"

   # validate checksum
   (Get-FileHash .\notation-azure-kv_${version}_windows_amd64.zip).Hash

   # install the plugin
   mkdir "$env:AppData\notation\plugins\azure-kv"
   Expand-Archive -Path notation-azure-kv_${version}_windows_amd64.zip -DestinationPath "$env:AppData\notation\plugins\azure-kv"
   ```
3. Run `notation plugin list` and confirm the `azure-kv` plugin is installed.

## Getting started
1. [Sign and verify an artifact with a self-signed Azure Key Vault certificate](docs/self-signed-workflow.md)
2. [Sign and verify an artifact with a certificate signed by a trusted CA in Azure Key Vault](docs/ca-signed-workflow.md)

> **Note** Please make sure the certificate is in PEM format. PCKS#12 will be supported in the future.
## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft
trademarks or logos is subject to and must follow
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
