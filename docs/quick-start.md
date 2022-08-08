# Quick Start

```bash
mkdir -p notation-akv-demo && cd notation-akv-demo
AKV_NAME=yzhakv
ACR_NAME=yzhregistry
REGISTRY=${ACR_NAME}.azurecr.io
REPO=net-monitor
TAG=v1
IMAGE=${REGISTRY_NAME}/${REPO}:${TAG}
KEY_NAME=test16
```

## Create the CA
Create a config file for openssl to create a valid CA
```bash
cat <<EOF > ./ca_ext.cnf
[ v3_ca ]
basicConstraints = CA:TRUE
keyUsage = critical,keyCertSign
extendedKeyUsage = codeSigning
EOF
```
Sign the CA
```bash
# create a signing request to sign your root CA
openssl req -new -newkey rsa:2048 -nodes -out ca.csr -keyout ca.key -extensions v3_ca

# sign the root CA with ca_ext.cnf
openssl x509 -signkey ca.key -days 365 -req -in ca.csr -set_serial 01 -out ca.crt -extensions v3_ca -extfile ./ca_ext.cnf
```

## Create the leaf certificate from Azure KeyVault
Create a certificate policy, which will be used by keyvault to create the leaf certificate
```bash
cat <<EOF > ./my_policy.json
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
    "subject": "CN=MYCA",
    "validityInMonths": 12
  }
}
EOF
```
Create your certificate
```bash
az keyvault certificate create -n ${KEY_NAME} --vault-name ${AKV_NAME} -p @my_policy.json
```

## Sign the leaf certificate
Download the certificate signing request(CSR)
```bash
CSR=$(az keyvault certificate pending show --vault-name ${AKV_NAME} --name ${KEY_NAME} --query 'csr' -o tsv)
CSR_PATH=${KEY_NAME}.csr
printf -- "-----BEGIN CERTIFICATE REQUEST-----\n%s\n-----END CERTIFICATE REQUEST-----\n" $CSR > ${CSR_PATH}
```

Create config file for openssl to sign the certificate
```bash
cat <<EOF > ./ext.cnf
[ v3_ca ]
keyUsage = critical,digitalSignature
extendedKeyUsage = codeSigning
EOF
```

Sign and merge the certificate chain
```bash
# sign your certificate by using the CA previously created
SIGNED_CERT_PATH=${KEY_NAME}.crt
openssl x509 -CA ca.crt -CAkey ca.key -days 365 -req -in ${CSR_PATH} -set_serial 02 -out ${SIGNED_CERT_PATH} -extensions v3_ca -extfile ./ext.cnf

# merge the certificate chain
CERTCHAIN_PATH=${KEY_NAME}-chain.pem
cat ${SIGNED_CERT_PATH} ca.crt > ${CERTCHAIN_PATH}
```

## Upload the leaf certificate to Azure KeyVault
```bash
az keyvault certificate pending merge --vault-name ${AKV_NAME} --name ${KEY_NAME} --file ${CERTCHAIN_PATH}
```

## Sign with notation

Login to the ACR
```bash
export NOTATION_USERNAME="00000000-0000-0000-0000-000000000000"
export NOTATION_PASSWORD=$(az acr login --name ${REGISTRY} --expose-token --output tsv --query accessToken)
```

Add key by using azure-kv plugin
```bash
KEY_ID=$(az keyvault certificate show -n ${KEY_NAME} --vault-name ${AKV_NAME} --query 'kid' -o tsv)
notation key add --name ${KEY_NAME} --plugin azure-kv --id ${KEY_ID}
notation key ls
```

Sign the image
```bash
notation sign --key ${KEY_NAME} ${IMAGE}
```

List remote signatures
```bash
notation ls ${IMAGE}
```

## Verify with notation
Download the certificate
```bash
CERT_ID=$(az keyvault certificate show -n ${KEY_NAME} --vault-name ${AKV_NAME} --query 'sid' -o tsv)
CERT_PATH=./${KEY_NAME}-cert.pem
az keyvault secret download --file ${CERT_PATH} --id ${CERT_ID}
```

Verify the image
```bash
notation cert add --name ${KEY_NAME} ${CERT_PATH}
notation cert ls 
notation verify --cert ${KEY_NAME} ${IMAGE}
```