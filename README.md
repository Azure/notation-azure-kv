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
   notation plugin install --url https://github.com/Azure/notation-azure-kv/releases/download/v1.0.2/notation-azure-kv_1.0.2_linux_amd64.tar.gz --sha256sum f2b2e131a435b6a9742c202237b9aceda81859e6d4bd6242c2568ba556cee20e
   ```

   For Linux arm64:
   ```
   notation plugin install --url https://github.com/Azure/notation-azure-kv/releases/download/v1.0.2/notation-azure-kv_1.0.2_linux_arm64.tar.gz --sha256sum 05cb2ca3460d07841f69b25d56fc7c93afe333b6b46ce33882a599cf0af9d532
   ```

   For Windows amd64:
   ```
   notation plugin install --url https://github.com/Azure/notation-azure-kv/releases/download/v1.0.2/notation-azure-kv_1.0.2_windows_amd64.zip --sha256sum 47769106233f9a4f34abed67d0ad6154ddee0e45b31c52127c23c52302970496
   ```

   For macOS amd64:
   ```
   notation plugin install --url https://github.com/Azure/notation-azure-kv/releases/download/v1.0.2/notation-azure-kv_1.0.2_darwin_amd64.tar.gz --sha256sum 9dfc197b2d03e2f0470c62997434cf6fa78476cc1364527025fe8a86acda94e3
   ```

   For macOS arm64:
   ```
   notation plugin install --url https://github.com/Azure/notation-azure-kv/releases/download/v1.0.2/notation-azure-kv_1.0.2_darwin_arm64.tar.gz --sha256sum ebc53b7ef1c32fb9d4ea7b85d30875b4595893d310a3ad7facfb333f55d5c3ed
   ```

   **Manually install**:

   For Linux Bash:
   ```bash
   version=1.0.2
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
   version=1.0.2
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
   $version = "1.0.2"
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
