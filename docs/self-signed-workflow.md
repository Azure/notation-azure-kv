# Sign and verify an artifact with a self-signed Azure Key Vault certificate
> [!WARNING]
> Using self-signed certificates are intended for development and testing. Outside of development and testing, a certificate from a trusted CA is recommended.

> [!NOTE]
> The following guide can be executed on Linux bash, macOS Zsh and Windows WSL
1. [Install the Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
2. Log in using the Azure CLI:
   ```sh
   az login
   az account set --subscription $subscriptionID
   ```
3. Create an Azure Key Vault:
   ```sh
   resourceGroup=<your-resource-group-name>
   keyVault=<your-key-vault-name>
   location=westus
   certName=notationSelfSignedCert
   ```
   Create a resource group
   ```sh
   az group create -n $resourceGroup -l $location
   ```
   Create a Azure Key Vault
   ```sh
   az keyvault create -l $location -n $keyVault --resource-group $resourceGroup
   ```
   Assign `Certificates Get` and `Key Sign` permission to your credentials:
   ```sh
   userId=$(az ad signed-in-user show --query id -o tsv)
   az keyvault set-policy -n "$keyVault" \
      --key-permissions sign \
      --certificates-permissions get \
      --upn "$userId"
   ```
> [!NOTE]
> The script assigns the permission to the current user, and you can also assign the permission to your [managed identity](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview) or [service principal](https://learn.microsoft.com/azure/active-directory/develop/app-objects-and-service-principals?tabs=browser).
> To know more about permission management, please visit [Azure Key Vualt access policy](https://learn.microsoft.com/azure/key-vault/general/assign-access-policy?tabs=azure-portal).
4. create a self-signed certificate:
   ```sh
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
       "subject": "CN=Test-Signer,C=US,ST=WA,O=notation",
       "validityInMonths": 12
     }
   }
   EOF

   # create self-signed certificate
   az keyvault certificate create -n $certName --vault-name $keyVault -p @selfSignedPolicy.json

   # get the key identifier
   keyID=$(az keyvault certificate show -n $certName --vault-name $keyVault --query 'kid' -o tsv)
   ```
> [!TIP]
> Versionless KeyID is also supported. For example: https://{vault-name}.vault.azure.net/certificates/{certificate-name}
5. [Create an Azure Container Registry](https://learn.microsoft.com/azure/container-registry/container-registry-get-started-portal?tabs=azure-cli). The remaining steps use the example login server `<registry-name>.azurecr.io`, but you must substitute your own login server value.
6. Log in to container registry and push an image for signing:
   ```sh
   registryName="<registry-name>"
   server="${registryName}.azurecr.io"
   
   az acr login --name $registryName
   # notation login $server  # if you don't use Azure Container Registry

   # push a hello-world image for signing
   docker pull hello-world:latest
   docker tag hello-world:latest $server/hello-world:v1
   docker push $server/hello-world:v1
   ```
7. Sign the container image with Notation:
   ```sh
   notation key add --plugin azure-kv --id $keyID akv-key --default --plugin-config self_signed=true
   notation sign $server/hello-world:v1
   ```

   The following example output shows the artifact is successfully signed.
   ```text
   Warning: Always sign the artifact using digest(@sha256:...) rather than a tag(:v1) because tags are mutable and a tag reference can point to a different artifact than the one signed.
   Successfully signed notation.azurecr.io/hello-world@sha256:f54a58bc1aac5ea1a25d796ae155dc228b3f0e11d046ae276b39c4bf2f13d8c4
   ```
8. Add the certificate to your trust store and the `trustpolicy.json` to Notation configuration directory:
   ```sh
   cat <<EOF > ./selfSignedCert.crt
   -----BEGIN CERTIFICATE-----
   $(az keyvault certificate show -n $certName --vault-name $keyVault --query 'cer' -o tsv)
   -----END CERTIFICATE-----
   EOF
   notation cert add --type ca --store selfSigned ./selfSignedCert.crt
   
   # add notation trust policy
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
9. Verify the signature associated with the image:
   ```sh
   notation verify $server/hello-world:v1
   ```
   The following output shows the artifact is successfully verified.
   ```text
   Warning: Always verify the artifact using digest(@sha256:...) rather than a tag(:v1) because resolved digest may not point to the same signed artifact, as tags are mutable.
   Successfully verified signature for notation.azurecr.io/hello-world@sha256:f54a58bc1aac5ea1a25d796ae155dc228b3f0e11d046ae276b39c4bf2f13d8c4
   ```
