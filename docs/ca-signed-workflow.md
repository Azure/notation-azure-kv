# Sign and verify an artifact with a certificate signed by a trusted CA in Azure Key Vault
> **Note** The following guide can be executed on Linux bash, macOS Zsh and Windows WSL
1. [Install the Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
2. Log in to Azure with Azure CLI, set the subscription and make sure the `GetCertificate` and `Sign` permission have been granted to your role:
   ```sh
   az login
   az account set --subscription $subscriptionID
   ```
3. Create an Azure Key Vault:
   ```sh
   resourceGroup=<your-resource-group-name>
   keyVault=<your-key-vault-name>
   location=westus
   certName=notationLeafCert

   # create a resource group
   az group create -n $resourceGroup -l $location
   
   # create a Azure Key Vault
   az keyvault create -l $location -n $keyVault --resource-group $resourceGroup
   ```
4. Create a Certificate Signing Request (CSR):
   ```sh
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
       "subject": "CN=Test-Signer,C=US,ST=WA,O=notation",
       "validityInMonths": 12
     }
   }
   EOF

   # create the leaf certificate
   az keyvault certificate create -n $certName --vault-name $keyVault -p @leafCert.json

   # get the CSR
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
   signedCertPath=${certName}.crt
   openssl x509 -CA ca.crt -CAkey ca.key -days 365 -req -in ${csrPath} -set_serial 02 -out ${signedCertPath} -extensions v3_ca -extfile ./ext.cnf
   ```

6. After you get the leaf certificate, you can merge the signed leaf certificate (`$signedCertPath`) or certificate chain to your Azure Key Vault:
   ```sh
   az keyvault certificate pending merge --vault-name $keyVault --name $certName --file $signedCertPath

   # get the key identifier
   keyID=$(az keyvault certificate show -n $certName --vault-name $keyVault --query 'kid' -o tsv)
   ```
7. [Create an Azure Container Registry](https://learn.microsoft.com/azure/container-registry/container-registry-get-started-portal?tabs=azure-cli). The remaining steps use the example login server `<registry-name>.azurecr.io`, but you must substitute your own login server value.
8. Log in to container registry and push an image for signing:
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
9. Sign the image with an external certificate bundle (`$certBundlePath`) including the intermediate certificates and a root certificate in PEM format. You may fetch the certificate bundle from your CA official site.
   > **Note** Check that the certificates in the PEM certificate bundle are arranged in the correct order: starting from the first intermediate certificate that signed the leaf certificate and ending with the root certificate.
   
   > **Note** If you have generated the certificate with `openssl` according to the above steps, the certificate bundle is the root certificate `ca.crt`.
   ```sh
   notation key add --plugin azure-kv --id $keyID akv-key --default
   notation sign $server/hello-world:v1 --plugin-config=ca_certs=$certBundlePath
   ```

   The following example output shows the artifact is successfully signed.
   ```sh
   Warning: Always sign the artifact using digest(@sha256:...) rather than a tag(:v1) because tags are mutable and a tag reference can point to a different artifact than the one signed.
   Successfully signed notation.azurecr.io/hello-world@sha256:f54a58bc1aac5ea1a25d796ae155dc228b3f0e11d046ae276b39c4bf2f13d8c4
   ```
10. Signature verification with Notation needs the root certificate of your CA in the trust store and a `trustpolicy.json` file in Notation configuration directory:
   ```sh
   # add root certificate ($rootCertPath) to notation trust store
   notation cert add --type ca --store trusted $rootCertPath
   
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
   ```sh
   Warning: Always verify the artifact using digest(@sha256:...) rather than a tag(:v1) because resolved digest may not point to the same signed artifact, as tags are mutable.
   Successfully verified signature for notation.azurecr.io/hello-world@sha256:f54a58bc1aac5ea1a25d796ae155dc228b3f0e11d046ae276b39c4bf2f13d8c4
   ```