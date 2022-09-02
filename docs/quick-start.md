# Quick Start

The Azure Key Vault (AKV) is used to store a signing key that can be utilized by notation with the notation AKV plugin (azure-kv) to sign and verify container images and other artifacts. The Azure Container Registry (ACR) allows you to attach these signatures using the az or oras CLI commands.

Please note this guide is a simple version of quick start. Please check out this [guide](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-tutorial-sign-build-push#verify-the-container-image) for details.  

## Prepare a working directoy and configure environment variables

Configure AKV, ACR and image resource names:

```bash
mkdir -p notation-akv-demo && cd notation-akv-demo
AKV_NAME=your_akv_name
ACR_NAME=your_acr_name
REGISTRY=${ACR_NAME}.azurecr.io
REPO=your_repo_name
TAG=your_tag_name
IMAGE=${REGISTRY}/${REPO}:${TAG}
KEY_NAME=your_key_name
```

## Create a CA for Testing Purpose

If you have an existing CA, upload it to AKV. For more information on how to use your own signing key, see the [signing certificate requirements](https://github.com/notaryproject/notaryproject/blob/main/signature-specification.md#certificate-requirements). Otherwise create a CA for remote signing using the steps below.

```bash
openssl req -x509 -sha256 -nodes -newkey rsa:2048 -keyout ca.key -out ca.crt -days 365 -subj "/CN=Test CA" -addext "keyUsage=critical,keyCertSign"
```

## Create a leaf certificate from Azure KeyVault

Create a certificate policy for Azure KeyVault to create the leaf certificate:

```bash
cat <<EOF > ./leaf_policy.json
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
```

Then create your own leaf certificate:

```bash
az keyvault certificate create -n ${KEY_NAME} --vault-name ${AKV_NAME} -p @leaf_policy.json
```

## Sign the leaf certificate

Download the certificate signing request (CSR) for your leaf certificate:

```bash
CSR=$(az keyvault certificate pending show --vault-name ${AKV_NAME} --name ${KEY_NAME} --query 'csr' -o tsv)
CSR_PATH=${KEY_NAME}.csr
printf -- "-----BEGIN CERTIFICATE REQUEST-----\n%s\n-----END CERTIFICATE REQUEST-----\n" $CSR > ${CSR_PATH}
```

Create a config file for openssl to sign the leaf certificate:

```bash
cat <<EOF > ./ext.cnf
[ v3_ca ]
keyUsage = critical,digitalSignature
extendedKeyUsage = codeSigning
EOF
```

Sign and merge the certificate chain:

```bash
# Sign your leaf certificate by using the CA that we created above
SIGNED_CERT_PATH=${KEY_NAME}.crt
openssl x509 -CA ca.crt -CAkey ca.key -days 365 -req -in ${CSR_PATH} -set_serial 02 -out ${SIGNED_CERT_PATH} -extensions v3_ca -extfile ./ext.cnf

# Merge the certificate chain
CERTCHAIN_PATH=${KEY_NAME}-chain.crt
cat ${SIGNED_CERT_PATH} ca.crt > ${CERTCHAIN_PATH}
```

## Upload the leaf certificate to Azure KeyVault
```bash
az keyvault certificate pending merge --vault-name ${AKV_NAME} --name ${KEY_NAME} --file ${CERTCHAIN_PATH}
```

## Sign the container image:

Log in to the ACR:

```bash
export NOTATION_PASSWORD=$(az acr login --name ${ACR_NAME} --expose-token --output tsv --query accessToken)
```

Get the Key ID for the certificate and add the Key ID to the keys and certs:

```bash
KEY_ID=$(az keyvault certificate show -n ${KEY_NAME} --vault-name ${AKV_NAME} --query 'kid' -o tsv)
notation key add --name ${KEY_NAME} --plugin azure-kv --id ${KEY_ID}
notation key ls
```

Choose an authorization mode to visit your keyvault from the plugin:

- Option 1: Authorized by Managed Identity (by default):
    ```bash
    export AKV_AUTH_METHOD="AKV_AUTH_FROM_MI"
    ```

    Make sure you have at least granted secret-get, certificate-get, key-sign permissions to your resources.

    The script below is a demo to check and set your Azure VM's access policy to your keyvault. For more details, please check [this guide](https://docs.microsoft.com/en-us/azure/key-vault/general/assign-access-policy?tabs=azure-portal) for details.
    
    ```bash
    # get your azure vm's principalId
    az vm list --query "[?name=='${VM_NAME}'].identity"

    # get your keyvault's access policy for your azure vm. $OID should be the principalId which we get from the above command
    az keyvault show --name ${AKV_NAME} --query "properties.accessPolicies[].{objectId:objectId,permissions:permissions}[?contains(objectId,'${OID}')]"

    # if policy doesn't meet the sign requirements, set the access policy.
    az keyvault set-policy --name ${AKV_NAME} --object-id ${OID} --secret-permissions get --key-permissions sign --certificate-permissions get
    ```
    
- Option 2: Authorized by Azure CLI 2.0:

    ```bash
    export AKV_AUTH_METHOD="AKV_AUTH_FROM_CLI"
    ```
    Login to your azure account by Azure CLI
    ```bash
    az login
    az account set --subscription ${subscriptionID}
    ```

Sign the container image:

```bash
notation sign --key ${KEY_NAME} ${IMAGE}
```

List the signatures:

```bash
notation ls ${IMAGE}
```

## Verify the container image

Download the certificate:

```bash
CERT_ID=$(az keyvault certificate show -n ${KEY_NAME} --vault-name ${AKV_NAME} --query 'sid' -o tsv)
CERT_PATH=${KEY_NAME}-cert.crt
az keyvault secret download --file ${CERT_PATH} --id ${CERT_ID}
```

Generate a CA certificate:

```bash
notation cert add --name ${KEY_NAME} ${CERT_PATH}
notation cert ls
```

Verify the container image:

```bash
notation verify --cert ${KEY_NAME} ${IMAGE}
```

You can use the notation verify command to ensure the container image hasn't been tampered with since build time.
