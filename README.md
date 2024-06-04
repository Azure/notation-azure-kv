# notation-azure-kv

[![codecov](https://codecov.io/gh/Azure/notation-azure-kv/branch/main/graph/badge.svg)](https://codecov.io/gh/Azure/notation-azure-kv)

Azure Provider for the [Notation CLI](https://github.com/notaryproject/notation)

The `notation-azure-kv` plugin allows you to sign the Notation-generated payload with a certificate in Azure Key Vault (AKV). The certificate and private key are stored in AKV and the plugin will request signing and obtain the leaf certificate from AKV.

The plugin supports several [credential types](./docs/plugin-config.md#credential_type).

## Installation the AKV plugin
Before you begin, make sure the latest version of the [Notation CLI has been installed](https://notaryproject.dev/docs/installation/cli/).

1. Navigate to the [Releases](https://github.com/Azure/notation-azure-kv/releases) page and choose a release of `notation-azure-kv`.
2. Download, verify, and then install the specified version of the plugin.

   **Automatically install**:

   For Notation >= v1.1.0, please use [notation plugin install](https://github.com/notaryproject/notation/blob/v1.1.0/specs/commandline/plugin.md#notation-plugin-install) command to automatically install azure-kv plugin.

   For Linux amd64:
   ```
   notation plugin install --url https://github.com/Azure/notation-azure-kv/releases/download/v1.2.0/notation-azure-kv_1.2.0_linux_amd64.tar.gz --sha256sum 06bb5198af31ce11b08c4557ae4c2cbfb09878dfa6b637b7407ebc2d57b87b34 
   ```

   For Linux arm64:
   ```
   notation plugin install --url https://github.com/Azure/notation-azure-kv/releases/download/v1.2.0/notation-azure-kv_1.2.0_linux_arm64.tar.gz --sha256sum 49520105bdb707a7bd3a519c55cc1541fbb336ab45b665c9cb2f84593f20ff29
   ```

   For Windows amd64:
   ```
   notation plugin install --url https://github.com/Azure/notation-azure-kv/releases/download/v1.2.0/notation-azure-kv_1.2.0_windows_amd64.zip --sha256sum 4478b4a323363e264924349bf7007d4201621024b0f9252e0cfde42c0938c667
   ```

   For macOS amd64:
   ```
   notation plugin install --url https://github.com/Azure/notation-azure-kv/releases/download/v1.2.0/notation-azure-kv_1.2.0_darwin_amd64.tar.gz --sha256sum 4b5a3480fc1d6844a69f7d9483530e9be24a74441fd571ccd5eec9f6a5337eed
   ```

   For macOS arm64:
   ```
   notation plugin install --url https://github.com/Azure/notation-azure-kv/releases/download/v1.2.0/notation-azure-kv_1.2.0_darwin_arm64.tar.gz --sha256sum 2bcfc0ee3ad1cbb1854110e3f02b89d46c14feb5736de37cd237b9668f378f2f
   ```

   **Manually install**:

   For Linux Bash:
   ```bash
   version=1.2.0
   arch=amd64
   install_path="${HOME}/.config/notation/plugins/azure-kv"

   # download tarball and checksum
   checksum_file="notation-azure-kv_${version}_checksums.txt"
   tar_file="notation-azure-kv_${version}_linux_${arch}.tar.gz"
   curl -Lo ${checksum_file} "https://github.com/Azure/notation-azure-kv/releases/download/v${version}/${checksum_file}"
   curl -Lo ${tar_file} "https://github.com/Azure/notation-azure-kv/releases/download/v${version}/${tar_file}"

   # validate checksum
   grep ${tar_file} ${checksum_file} | sha256sum -c

   # install the plugin
   mkdir -p ${install_path}
   tar xvzf ${tar_file} -C ${install_path} notation-azure-kv
   ```

   For macOS Zsh:
   ```zsh
   version=1.2.0
   arch=arm64
   install_path="${HOME}/Library/Application Support/notation/plugins/azure-kv"

   # download tarball and checksum
   checksum_file="notation-azure-kv_${version}_checksums.txt"
   tar_file="notation-azure-kv_${version}_darwin_${arch}.tar.gz"
   curl -Lo ${checksum_file} "https://github.com/Azure/notation-azure-kv/releases/download/v${version}/${checksum_file}"
   curl -Lo ${tar_file} "https://github.com/Azure/notation-azure-kv/releases/download/v${version}/${tar_file}"

   # validate checksum
   grep ${tar_file} ${checksum_file} | shasum -a 256 -c

   # install the plugin
   mkdir -p ${install_path}
   tar xvzf ${tar_file} -C ${install_path} notation-azure-kv
   ```

   For Windows Powershell:
   ```powershell
   $version = "1.2.0"
   $arch = "amd64"
   $install_path = "${env:AppData}\notation\plugins\azure-kv"

   # download zip file and checksum
   $checksum_file = "notation-azure-kv_${version}_checksums.txt"
   $zip_file = "notation-azure-kv_${version}_windows_${arch}.zip"
   Invoke-WebRequest -OutFile ${checksum_file} "https://github.com/Azure/notation-azure-kv/releases/download/v${version}/${checksum_file}"
   Invoke-WebRequest -OutFile ${zip_file} "https://github.com/Azure/notation-azure-kv/releases/download/v${version}/${zip_file}"

   # validate checksum
   $checksum = (Get-Content ${checksum_file} | Select-String -List ${zip_file}).Line.Split() | Where-Object {$_}
   If ($checksum[0] -ne (Get-FileHash -Algorithm SHA256 $checksum[1]).Hash) {
      throw "$($checksum[1]): Failed"
   }

   # install the plugin
   mkdir ${install_path}
   Expand-Archive -Path ${zip_file} -DestinationPath ${install_path}
   ```
3. Run `notation plugin list` and confirm the `azure-kv` plugin is installed.

## Getting started
1. [Sign and verify an artifact with a self-signed Azure Key Vault certificate](docs/self-signed-workflow.md)
2. [Sign and verify an artifact with a certificate signed by a trusted CA in Azure Key Vault](docs/ca-signed-workflow.md)
3. [Plugin configuration](docs/plugin-config.md)

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit <https://cla.opensource.microsoft.com>.

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
