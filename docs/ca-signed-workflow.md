# Sign and verify an artifact with a certificate signed by a trusted CA in Azure Key Vault

> **Note**
> The following guide can be executed on Linux bash, macOS Zsh and Windows WSL
1. [Install the Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
2. Log in to Azure with Azure CLI:
   ```sh
   az login
   az account set --subscription $subscriptionID
   ```
3. Create an Azure Key Vault and assign permissions:
   ```sh
   resourceGroup=<your-resource-group-name>
   keyVault=<your-key-vault-name>
   location=westus
   certName=notationLeafCert
   ```
   Create a resource group
   ```sh
   az group create -n $resourceGroup -l $location
   ```
   Create a Azure Key Vault
   ```sh
   az keyvault create -l $location -n $keyVault --resource-group $resourceGroup
   ```
   Assign `Secrets Get` and `Key Sign` permission to your credentials:
   ```sh
   userId=$(az ad signed-in-user show --query id -o tsv)
   az keyvault set-policy -n "$keyVault" \
      --key-permissions sign \
      --secret-permissions get \
      --upn "$userId"
   ```
   > **Note** The script assigns the permission to the current user, and you can also assign the permission to your [managed identity](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview) or [service principal](https://learn.microsoft.com/azure/active-directory/develop/app-objects-and-service-principals?tabs=browser).
   > To know more about permission management, please visit [Azure Key Vualt access policy](https://learn.microsoft.com/azure/key-vault/general/assign-access-policy?tabs=azure-portal).
4. Create a Certificate Signing Request (CSR):

   Generate certificate policy:
   ```sh
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
       "subject": "CN=Test-Signer,C=US,ST=WA,O=notation",
       "validityInMonths": 12
     }
   }
   EOF
   ```
   Create the leaf certificate:
   ```sh
   az keyvault certificate create -n $certName --vault-name $keyVault -p @leafCert.json
   ```
   Get the CSR:
   ```sh
   csr=$(az keyvault certificate pending show --vault-name $keyVault --name $certName --query 'csr' -o tsv)
   csrPath=${certName}.csr
   printf -- "-----BEGIN CERTIFICATE REQUEST-----\n%s\n-----END CERTIFICATE REQUEST-----\n" $csr > ${csrPath}
   ```
5. Please take `${certName}.csr` file to a trusted CA to sign and issue your certificate, or you can use `openssl` tool to sign it locally for testing. Here is an example by using `openssl`:
   Create a private key and certificate for a root CA with `openssl`:
   ```sh
   openssl req -x509 -sha256 -nodes -newkey rsa:2048 -keyout ca.key -out ca.crt -days 365 -subj "/CN=Test CA" -addext "keyUsage=critical,keyCertSign"
   ```
   Create a configuration file. It will be used for `openssl` to sign the leaf certificate:
   ```sh
   cat <<EOF > ./ext.cnf
   [ v3_ca ]
   keyUsage = critical,digitalSignature
   extendedKeyUsage = codeSigning
   EOF
   ```
   Sign the certificate:
   ```sh
   signedCertPath="${certName}.crt"
   openssl x509 -CA ca.crt -CAkey ca.key -days 365 -req -in ${csrPath} -set_serial 02 -out ${signedCertPath} -extensions v3_ca -extfile ./ext.cnf
   ```
   Build the certificate chain:
   ```sh
   certChainPath="${certName}-chain.crt"
   cat "$signedCertPath" ca.crt > "$certChainPath"
   ```
   > **Note** If you have merged your certificate to Azure Key Vault without certificate chain or you don't want the plugin access your certificate chain with the `Secrets Get` permission, please use [ca_certs](./plugin-config.md#ca_certs) plugin configuration argument instead.

6. After you get the leaf certificate, you can merge the certificate chain (file at `$certChainPath`) to your Azure Key Vault:
   ```sh
   az keyvault certificate pending merge --vault-name $keyVault --name $certName --file $certChainPath
   ```
   Get the key identifier
   ```sh
   keyID=$(az keyvault certificate show -n $certName --vault-name $keyVault --query 'kid' -o tsv)
   ```
7. [Create an Azure Container Registry](https://learn.microsoft.com/azure/container-registry/container-registry-get-started-portal?tabs=azure-cli). The remaining steps use the example login server `<registry-name>.azurecr.io`, but you must substitute your own login server value.
8. Log in to container registry and push an image for signing:
   ```sh
   registryName="<registry-name>"
   server="${registryName}.azurecr.io"
   
   az acr login --name $registryName
   # notation login $server  # if you don't use Azure Container Registry
   ```
   Push a hello-world image for signing
   ```sh
   docker pull hello-world:latest
   docker tag hello-world:latest $server/hello-world:v1
   docker push $server/hello-world:v1
   ```
9. Sign the image
   ```sh
   notation key add --plugin azure-kv --id $keyID akv-key --default
   notation sign $server/hello-world:v1
   ```

   The following example output shows the artifact is successfully signed.
   ```text
   Warning: Always sign the artifact using digest(@sha256:...) rather than a tag(:v1) because tags are mutable and a tag reference can point to a different artifact than the one signed.
   Successfully signed notation.azurecr.io/hello-world@sha256:f54a58bc1aac5ea1a25d796ae155dc228b3f0e11d046ae276b39c4bf2f13d8c4
   ```
10. Signature verification with Notation needs the root certificate of your CA in the trust store and a `trustpolicy.json` file in Notation configuration directory:

    Add root certificate ($rootCertPath) to notation trust store
    ```sh
    notation cert add --type ca --store trusted $rootCertPath
    ```

    Add notation trust policy
    ```sh
    notationConfigDir="${HOME}/.config/notation"                        # for Linux and WSL
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
11. Verify the signature associated with the image:
    ```sh
    notation verify $server/hello-world:v1
    ```
    The following output shows the artifact is successfully verified.
    ```text
    Warning: Always verify the artifact using digest(@sha256:...) rather than a tag(:v1) because resolved digest may not point to the same signed artifact, as tags are mutable.
    Successfully verified signature for notation.azurecr.io/hello-world@sha256:f54a58bc1aac5ea1a25d796ae155dc228b3f0e11d046ae276b39c4bf2f13d8c4
    ```