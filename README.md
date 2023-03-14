# notation-azure-kv

[![codecov](https://codecov.io/gh/Azure/notation-azure-kv/branch/main/graph/badge.svg)](https://codecov.io/gh/Azure/notation-azure-kv)

Azure Provider for the [Notation CLI](https://github.com/notaryproject/notation)

The `notation-azure-kv` plugin provides the capability to sign the Notation generated payload by using Azure Key Vault (AKV). The user's certificate and private key should be stored in AKV and the plugin will request signing and getting the leaf certificate from AKV. 

The plugin supports Azure CLI identity and Managed Identity for accessing AKV.

## Installation
Before installing notation azure key vault plugin, please make sure Notation CLI has been installed. If you didn't install it, please follow the [Notation installation guide](https://notaryproject.dev/docs/installation/cli/).

1. Navigate to the [Releases](https://github.com/Azure/notation-azure-kv/releases) page and select the latest release of `notation-azure-kv`. The `notation-azure-kv` binaries for each platform are available under the Assets section. Please download it based on your own platform. 
2. Validate the checksum which should be the same as the one in `checksums.txt` and then install the plugin.
   
   For Linux Bash:
   ```sh
   version=0.6.0

   # validate checksum
   sha256sum notation-azure-kv_${version}_linux_amd64.tar.gz

   # install the plugin
   mkdir -p "$HOME/.config/notation/plugins/azure-kv"
   tar zxf notation-azure-kv_<version>_linux_amd64.tar.gz -C "$HOME/.config/notation/plugins/azure-kv" notation-azure-kv
   ```
   For MacOS Zsh:
   ```sh
   version=0.6.0

   # validate checksum
   shasum -a 256 notation-azure-kv_${version}_linux_amd64.tar.gz

   # install the plugin
   mkdir -p "$HOME/Library/Application Support/notation/plugins/azure-kv"
   tar zxf notation-azure-kv_<version>_darwin_amd64.tar.gz -C "$HOME/Library/Application Support/notation/plugins/azure-kv" notation-azure-kv
   ```
   For Windows Powershell:
   ```powershell
   $version = 0.6.0

   # validate checksum
   (Get-FileHash .\notation-azure-kv_$version_windows_amd64.zip).Hash

   # install the plugin
   mkdir "$env:AppData\notation\plugins\azure-kv"
   Expand-Archive -Path notation-azure-kv_<version>_windows_amd64.zip -DestinationPath "$env:AppData\notation\plugins\azure-kv"
   ```
3. Try to run `notation plugin list` to show the installed plugin.

## Getting started with a self-signed Azure Key Vault Certificate
> **Note** It is suggested to get a certificate from a trusted CA since a self-signed certificate is not publicly trusted.
1. Install Azure CLI by following the [guide](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
2. Login to Azure with Azure CLI, set the subscription and make sure the `GetCertificate` and `Sign` permission have been granted to your role:
   ```sh
   az login
   az account set --subscription $subscriptionID
   ```
3. Create an Azure Key Vault and a self-signed certificate:
   ```sh
   resourceGroup=notationResource
   keyVault=notationKV
   location=westus
   certName=notationSelfSignedCert

   # create a resource group
   az group create -n $resourceGroup -l $location
   
   # create a Azure Key Vault
   az keyvault create -l $location -n $keyVault --resource-group $resourceGroup

   # generate certificate policy
   cat <<EOF > ./selfSignedPolicy.json
   {
     "issuerParameters": {
       "certificateTransparency": null,
       "name": "Self"
     },
     "keyProperties": {
       "curve": null,
       "exportable": false,
       "keySize": 2048,
       "keyType": "RSA",
       "reuseKey": true
     },
     "secretProperties": {
       "contentType": "application/x-pem-file"
     },
     "x509CertificateProperties": {
        "ekus": [
           "1.3.6.1.5.5.7.3.3"
       ],
       "keyUsage": [
         "digitalSignature"
       ],
       "subject": "CN=Test Signer",
       "validityInMonths": 12
     }
   }
   EOF

   # create self-signed certificate
   az keyvault certificate create -n $certName --vault-name $keyVault -p @selfSignedPolicy.json

   # get the key identifier
   keyID=$(az keyvault certificate show -n $certName --vault-name $keyVault --query 'kid' -o tsv)
   ```
4. Prepare a container registry. You can follow the [guide](https://learn.microsoft.com/azure/container-registry/container-registry-get-started-portal?tabs=azure-cli) to create an Azure Container Registry which is recommended. Suppose the logging server is `notation.azurecr.io` with an `hello-world:v1` image.
5. Sign the container image with Notation:
   ```sh
   notation sign notation.azurecr.io/hello-world:v1 --id $keyID --plugin azure-kv
   ```

   example output
   ```
   Warning: Always sign the artifact using digest(@sha256:...) rather than a tag(:v1) because tags are mutable and a tag reference can point to a different artifact than the one signed.
   Successfully signed notation.azurecr.io/hello-world@sha256:f54a58bc1aac5ea1a25d796ae155dc228b3f0e11d046ae276b39c4bf2f13d8c4
   ```
6. Verify the signature associated with the image:
   ```sh
   # add self-signed certificate to notation trust store
   cat <<EOF > ./selfSignedCert.crt
   -----BEGIN CERTIFICATE-----
   $(az keyvault certificate show -n $certName --vault-name $keyVault --query 'cer' -o tsv)
   -----END CERTIFICATE-----
   EOF
   notation cert add --type ca --store selfSigned ./selfSignedCert.crt
   
   # add notation trust policy
   notationConfigDir="${HOME}/.config/notation"                      # for Linux
   # notationConfigDir="${HOME}/Library/Application Support/notation"  # for macOS

   mkdir -p $notationConfigDir
   cat <<EOF > $notationConfigDir/trustpolicy.json
   {
    "version": "1.0",
    "trustPolicies": [
        {
            "name": "trust-policy-example",
            "registryScopes": [ "*" ],
            "signatureVerification": {
                "level" : "strict" 
            },
            "trustStores": [ "ca:selfSigned" ],
            "trustedIdentities": [
                "*"
            ]
        }
    ]
   }
   EOF
   chmod 600 $notationConfigDir/trustpolicy.json
   ```

   verify signature
   ```
   notation verify notation.azurecr.io/hello-world:v1
   ```
   example output
   ```
   Warning: Always verify the artifact using digest(@sha256:...) rather than a tag(:v1) because resolved digest may not point to the same signed artifact, as tags are mutable.
   Successfully verified signature for notation.azurecr.io/hello-world@sha256:f54a58bc1aac5ea1a25d796ae155dc228b3f0e11d046ae276b39c4bf2f13d8c4
   ```

## Getting started with a certificate signed by a trusted CA
1. Install Azure CLI by following the [guide](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
2. Login to Azure with Azure CLI, set the subscription and make sure the `GetCertificate` and `Sign` permission have been granted to your role:
   ```sh
   az login
   az account set --subscription $subscriptionID
   ```
3. Create an Azure Key Vault and a Certificate Signing Request (CSR):
   ```sh
   resourceGroup=notationResource
   keyVault=notationKV
   location=westus
   certName=notationLeafCert

   # create a resource group
   az group create -n $resourceGroup -l $location
   
   # create a Azure Key Vault
   az keyvault create -l $location -n $keyVault --resource-group $resourceGroup

   # generate certificate policy
   cat <<EOF > ./leafCert.json
   {
     "issuerParameters": {
       "certificateTransparency": null,
       "name": "Unknown"
     },
     "keyProperties": {
       "curve": null,
       "exportable": false,
       "keySize": 2048,
       "keyType": "RSA",
       "reuseKey": true
     },
     "secretProperties": {
       "contentType": "application/x-pem-file"
     },
     "x509CertificateProperties": {
       "ekus": [
           "1.3.6.1.5.5.7.3.3"
       ],
       "keyUsage": [
         "digitalSignature"
       ],
       "subject": "CN=Test Signer",
       "validityInMonths": 12
     }
   }
   EOF

   # create the leaf certificate
   az keyvault certificate create -n $certName --vault-name $keyVault -p @leafCert.json

   # get the CSR
   CSR=$(az keyvault certificate pending show --vault-name $keyVault --name $certName --query 'csr' -o tsv)
   CSR_PATH=${certName}.csr
   printf -- "-----BEGIN CERTIFICATE REQUEST-----\n%s\n-----END CERTIFICATE REQUEST-----\n" $CSR > ${CSR_PATH}
   ```
4. Please take `${certName}.csr` file to a trusted CA to sign and issue your certificate, or you can use `openssl` tool to sign it locally for testing.
5. After you get the leaf certificate, you can merge the leaf certificate (`$leafCert`) to your Azure Key Vault:
   ```sh
   az keyvault certificate pending merge --vault-name $keyVault --name $certName --file $leafCert

   # get the key identifier
   keyID=$(az keyvault certificate show -n $certName --vault-name $keyVault --query 'kid' -o tsv)
   ```
6. Prepare a container registry. You can follow the [guide](https://learn.microsoft.com/en-us/azure/container-registry/container-registry-get-started-portal?tabs=azure-cli) to create an Azure Container Registry which is recommended. Suppose the logging server is `notation.azurecr.io` with an `hello-world:v1` image.
7. Sign the image with an external certificate bundle (`$certBundlePath`) including the intermediate certificates and a root certificate in PEM format. You may fetch the certificate bundle from your CA official website.
   ```sh
   notation sign notation.azurecr.io/hello-world:v1 --id $keyID --plugin azure-kv --plugin-config=ca_certs=$certBundlePath
   ```

   example output
   ```
   Warning: Always sign the artifact using digest(@sha256:...) rather than a tag(:v1) because tags are mutable and a tag reference can point to a different artifact than the one signed.
   Successfully signed notation.azurecr.io/hello-world@sha256:f54a58bc1aac5ea1a25d796ae155dc228b3f0e11d046ae276b39c4bf2f13d8c4
   ```
8. Signature verification with Notation needs the root certificate of your CA in the trust store:
   ```sh
   # add root certificate ($rootCertPath) to notation trust store
   notation cert add --type ca --store trusted $rootCertPath
   
   # add notation trust policy
   notationConfigDir="${HOME}/.config/notation"                      # for Linux
   # notationConfigDir="${HOME}/Library/Application Support/notation"  # for macOS

   mkdir -p $notationConfigDir
   cat <<EOF > $notationConfigDir/trustpolicy.json
   {
    "version": "1.0",
    "trustPolicies": [
        {
            "name": "trust-policy-example",
            "registryScopes": [ "*" ],
            "signatureVerification": {
                "level" : "strict" 
            },
            "trustStores": [ "ca:trusted" ],
            "trustedIdentities": [
                "*"
            ]
        }
    ]
   }
   EOF
   chmod 600 $notationConfigDir/trustpolicy.json
   ```

   verify signature
   ```
   notation verify notation.azurecr.io/hello-world:v1
   ```

   example output
   ```
   Warning: Always verify the artifact using digest(@sha256:...) rather than a tag(:v1) because resolved digest may not point to the same signed artifact, as tags are mutable.
   Successfully verified signature for notation.azurecr.io/hello-world@sha256:f54a58bc1aac5ea1a25d796ae155dc228b3f0e11d046ae276b39c4bf2f13d8c4
   ```
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
