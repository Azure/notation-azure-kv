# notation-azure-kv

[![codecov](https://codecov.io/gh/Azure/notation-azure-kv/branch/main/graph/badge.svg)](https://codecov.io/gh/Azure/notation-azure-kv)

Azure Provider for the [Notation CLI](https://github.com/notaryproject/notation)

The notation-azure-kv plugin provides the capability to signing the Notation generated payload by using Azure Key Vault (AKV). The user's certificate and private key should be stored in AKV and the plugin will request signing and getting the certificate from AKV. 

The plugin supports Azure CLI identity and Managed Identity for accessing AKV.

## Installation
Install the latest released Notation CLI and Azure-kv plugin via the command-line with curl
```sh
sh -c "$(curl -fsSL https://raw.githubusercontent.com/Azure/notation-azure-kv/main/tools/install.sh)" -- notation azure-kv
```
```
# example output
Installing on Linux amd64
Collecting notation latest release...
notation_1.0.0-rc.3_linux_amd64.tar.gz               100%[===========>]   3.31M   673KB/s    in 5.0s    
Sucessfully installed notation-v1.0.0-rc.3 to /home/exampleuser/bin
Run the command to add the notation to PATH:
  export PATH=$PATH:/home/exampleuser/bin

Collecting notation-azure-kv latest release...
notation-azure-kv_0.5.0-rc.1_Linux_amd64.tar.gz      100%[===========>]   3.02M   873KB/s    in 3.5s    
Successfully installed notation-azure-kv-v0.5.0-rc.1 to /home/exampleuser/.config/notation/plugins/azure-kv
Run the command to show the installed plugins:
  /home/exampleuser/bin/notation plugin list
```
- if you only install Notation CLI, please execute the command:
```sh
sh -c "$(curl -fsSL https://raw.githubusercontent.com/Azure/notation-azure-kv/main/tools/install.sh)" -- notation
```

- if you only install Azure-kv plugin, please execute the command:
```sh
sh -c "$(curl -fsSL https://raw.githubusercontent.com/Azure/notation-azure-kv/main/tools/install.sh)" -- azure-kv
```
> The installation script only supports Linux and MacOS based on amd64 or arm64 architecture. For Windows amd64 users to install Azure-kv plugin, please download the latest released binary for Windows and extract the `notation-azure-kv.exe` binary from the Zip file to `%AppData%\notation\plugins\azure-kv` directory manually.
## Getting Started with a self-signed Azure Key Vault Certificate
To demonstrate the basic use case, starting with a self-signed certificate is easier to understand how the Azure-kv plugin works with Notation, however, you **should not** use this in production because the self-signed certificate is not trusted.
1. install Azure CLI by following the [guide](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
2. login with Azure CLI and set the subscription:
   ```sh
   az login
   az account set --subscription $subscriptionID
   ```
3. create an Azure Key Vault and a self-signed certificate:
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
4. run a local registry and push an image to be signed:
   ```sh
   # run a local registry with Docker
   docker run --rm -d -p 5000:5000 ghcr.io/oras-project/registry:v1.0.0-rc.3
   
   # push a hello-world image
   docker pull hello-world
   docker tag hello-world:latest localhost:5000/hello-world:v1
   docker push localhost:5000/hello-world:v1
   ```
5. notation sign:
   ```sh
   # sign with azure-kv
   notation sign localhost:5000/hello-world:v1 --id $keyID --plugin azure-kv
   # example output
   Warning: Always sign the artifact using digest(@sha256:...) rather than a tag(:v1) because tags are mutable and a tag reference can point to a different artifact than the one signed.
   Successfully signed localhost:5000/hello-world@sha256:f54a58bc1aac5ea1a25d796ae155dc228b3f0e11d046ae276b39c4bf2f13d8c4
   ```
6. notation verify:
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
   notationConfigDir="${HOME}/Library/Application Support/notation"  # for MacOS

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

   # verify signature
   notation verify localhost:5000/hello-world:v1
   # example output
   Warning: Always verify the artifact using digest(@sha256:...) rather than a tag(:v1) because resolved digest may not point to the same signed artifact, as tags are mutable.
   Successfully verified signature for localhost:5000/hello-world@sha256:f54a58bc1aac5ea1a25d796ae155dc228b3f0e11d046ae276b39c4bf2f13d8c4
   ```

## Uninstall
Uninstall Azure-kv plugin via command-line with curl:
```sh
sh -c "$(curl -fsSL https://raw.githubusercontent.com/Azure/notation-azure-kv/main/tools/uninstall.sh)" -- azure-kv
```
- if you uninstall both Notation and Azure-kv plugin, please execute the command:
```sh
sh -c "$(curl -fsSL https://raw.githubusercontent.com/Azure/notation-azure-kv/main/tools/uninstall.sh)" -- notation azure-kv
```

To clean up Notation configurations, please execute the command:
```sh
sh -c "$(curl -fsSL https://raw.githubusercontent.com/Azure/notation-azure-kv/main/tools/uninstall.sh)" -- config
```

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
