#!/bin/bash
# This script generates various certificates on Azure Key Vault for E2E testing
# It may be used when initial setup is needed or when certificates need to 
# be regenerated.
#
# Prerequisites:
#   - Azure CLI
#   - OpenSSL
#   - an existing Azure Key Vault
#   - env AKV_SUB: Azure Key Vault subscription
#   - env AKV_NAME: Azure Key Vault name

set -ex

# check Azure Key Vault subscription
if [ -z "$AKV_SUB" ]; then
  echo "AKV_SUB is not set"
  exit 1
fi

# check Azure Key Vault name
if [ -z "$AKV_NAME" ]; then
  echo "AKV_NAME is not set"
  exit 1
fi

if ! command -v az &> /dev/null
then
  echo "az command could not be found"
  exit 1
fi

if ! command -v openssl &> /dev/null
then
  echo "openssl command could not be found"
  exit 1
fi


# Generate self-signed PKCS12 certificate
function createSelfSignedPKCS12() {
  local certName
  certName="self-signed-pkcs12"
  local policy
  policy=$(cat << EOF
{
  "issuerParameters": {
    "name": "Self"
  },
  "keyProperties": {
    "exportable": false,
    "keySize": 2048,
    "keyType": "RSA",
    "reuseKey": true
  },
  "secretProperties": {
    "contentType": "application/x-pkcs12"
  },
  "x509CertificateProperties": {
    "ekus": [
        "1.3.6.1.5.5.7.3.3"
    ],
    "keyUsage": [
      "digitalSignature"
    ],
    "subject": "CN=Test-Signer,C=US,ST=WA,O=notation",
    "validityInMonths": 1200
  }
}
EOF
)
  az keyvault certificate create -n "$certName" --vault-name "$AKV_NAME" -p "$policy"
  echo "created ${certName}"
}

function createSelfSignedPEM() {
  local certName
  certName="self-signed-pem"
  local policy
  policy=$(cat << EOF
{
  "issuerParameters": {
    "name": "Self"
  },
  "keyProperties": {
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
    "validityInMonths": 1200
  }
}
EOF
)
  az keyvault certificate create -n "$certName" --vault-name "$AKV_NAME" -p "$policy"
  echo "created ${certName}"
}

# Generate CA issued PKCS12 certificate chain
# leaf.crt -> inter2.crt -> inter1.crt -> ca.crt
function generateCertChain() {
  # generate CA key and certificate
  echo "Generating CA key and certificate..."
  openssl genrsa -out ca.key 2048
  openssl req -new -x509 -days 36500 -key ca.key -subj "/O=Notation/CN=Notation Root CA" -out ca.crt -addext "keyUsage=critical,keyCertSign"

  # generate intermediate ca 1
  echo "Gneerating intermediate CA 1"
  openssl req -newkey rsa:2048 -nodes -keyout inter1.key -subj "/CN=Notation.inter1" -out inter1.csr
  openssl x509 -req -extfile <(printf "basicConstraints=critical,CA:TRUE\nkeyUsage=critical,keyCertSign") -days 36500 -in inter1.csr -CA ca.crt -CAkey ca.key -CAcreateserial -out inter1.crt

  # generate intermediate ca 2
  echo "Gneerating intermediate CA 2"
  openssl req -newkey rsa:2048 -nodes -keyout inter2.key -subj "/CN=Notation.inter2" -out inter2.csr
  openssl x509 -req -extfile <(printf "basicConstraints=critical,CA:TRUE\nkeyUsage=critical,keyCertSign") -days 36500 -in inter2.csr -CA inter1.crt -CAkey inter1.key -CAcreateserial -out inter2.crt

  # generate leaf key and certificate
  echo "Generating leaf key and certificate..."
  # openssl genrsa -out leaf.key 2048
  openssl req -newkey rsa:2048 -nodes -keyout leaf.key -subj "/CN=Test-Signer/C=US/ST=WA/O=notation" -out leaf.csr
  openssl x509 -req -extfile <(printf "basicConstraints=critical,CA:FALSE\nkeyUsage=critical,digitalSignature") -days 36500 -in leaf.csr -CA inter2.crt -CAkey inter2.key -CAcreateserial -out leaf.crt

  # generate PEM certificate chain
  cat leaf.key leaf.crt inter2.crt inter1.crt ca.crt > cert-chain.pem
  cat leaf.key leaf.crt ca.crt inter1.crt inter2.crt  > unordered-cert-chain.pem

  # generate PKCS12 certificate chain
  openssl pkcs12 -export -out cert-chain.pfx -inkey leaf.key -in cert-chain.pem
  openssl pkcs12 -export -out unordered-cert-chain.pfx -inkey leaf.key -in unordered-cert-chain.pem

  # generate partial PEM certificate chain
  cat leaf.key leaf.crt > partial-cert-chain.pem
  # generate partial PKCS12 certificate chain
  openssl pkcs12 -export -out partial-cert-chain.pfx -inkey leaf.key -in partial-cert-chain.pem
  # create cert bundle for e2e
  cat inter2.crt inter1.crt ca.crt > cert-bundle.pem
}

function importCAIssuedPKCS12CertificateChain() {
  local certName
  certName="imported-ca-issued-pkcs12"
  local unorderedCertName
  unorderedCertName="imported-ca-issued-pkcs12-unordered"
  local policy=$(cat << EOF
{
  "keyProperties": {
    "exportable": false
  },
  "secretProperties": {
    "contentType": "application/x-pkcs12"
  },
  "x509CertificateProperties": {
    "ekus": [
        "1.3.6.1.5.5.7.3.3"
    ]
  }
}
EOF
)
  az keyvault certificate import --file ./cert-chain.pfx -n "$certName" --vault-name "$AKV_NAME" --policy "$policy"
  echo "imported ${certName}"
  az keyvault certificate import --file ./unordered-cert-chain.pfx -n "$unorderedCertName" --vault-name "$AKV_NAME" --policy "$policy"
  echo "imported ${unorderedCertName}"
}

function importCAIssuedPEMCertificateChain() {
  local certName
  certName="imported-ca-issued-pem"
  local unorderedCertName
  unorderedCertName="imported-ca-issued-pem-unordered"
  local policy=$(cat << EOF
{
  "keyProperties": {
    "exportable": false
  },
  "secretProperties": {
    "contentType": "application/x-pem-file"
  },
  "x509CertificateProperties": {
    "ekus": [
        "1.3.6.1.5.5.7.3.3"
    ]
  }
}
EOF
)
  az keyvault certificate import --file ./cert-chain.pem -n "$certName" --vault-name "$AKV_NAME" --policy "$policy"
  echo "imported ${certName}"
  az keyvault certificate import --file ./unordered-cert-chain.pem -n "$unorderedCertName" --vault-name "$AKV_NAME" --policy "$policy"
  echo "imported ${unorderedCertName}"
}

function createPKCS12CertificateChainWithCSR() {
  local certName
  certName="csr-ca-issued-pkcs12-chain"
  local policy
  policy=$(cat << EOF
{
  "issuerParameters": {
    "name": "Unknown"
  },
  "keyProperties": {
    "exportable": false,
    "keySize": 2048,
    "keyType": "RSA",
    "reuseKey": true
  },
  "secretProperties": {
    "contentType": "application/x-pkcs12"
  },
  "x509CertificateProperties": {
    "ekus": [
        "1.3.6.1.5.5.7.3.3"
    ],
    "keyUsage": [
      "digitalSignature"
    ],
    "subject": "CN=Test-Signer,C=US,ST=WA,O=notation",
    "validityInMonths": 1200
  }
}
EOF
)
  az keyvault certificate create -n "$certName" --vault-name "$AKV_NAME" -p "$policy"
  echo "created ${certName} with CSR"

  # download CSR
  local csr
  csr=$(az keyvault certificate pending show --vault-name "$AKV_NAME" --name "$certName" --query "csr" -o tsv)
  local csrPath
  csrPath=${certName}.csr
  printf -- "-----BEGIN CERTIFICATE REQUEST-----\n%s\n-----END CERTIFICATE REQUEST-----\n" "$csr" > ${csrPath}
  local signedCertPath
  signedCertPath="${certName}.crt"

  # issue certificate chain with CSR
  openssl x509 -req -extfile <(printf "basicConstraints=critical,CA:FALSE\nkeyUsage=critical,digitalSignature") -days 36500 -in $csrPath -CA inter2.crt -CAkey inter2.key -CAcreateserial -out $signedCertPath
  certChainPath="${certName}-chain.crt"
  cat "$signedCertPath" inter2.crt inter1.crt ca.crt > "$certChainPath"

  # merge certificate chain
  az keyvault certificate pending merge --vault-name "$AKV_NAME" --name "$certName" --file "$certChainPath"
  echo "merged ${certName}"
}

function createPEMCertificateChainWithCSR() {
  local certName
  certName="csr-ca-issued-pem-chain"
  local policy
  policy=$(cat << EOF
{
  "issuerParameters": {
    "name": "Unknown"
  },
  "keyProperties": {
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
    "validityInMonths": 1200
  }
}
EOF
)
  az keyvault certificate create -n "$certName" --vault-name "$AKV_NAME" -p "$policy"
  echo "created ${certName} with CSR"

  # download CSR
  local csr
  csr=$(az keyvault certificate pending show --vault-name $AKV_NAME --name $certName --query "csr" -o tsv)
  local csrPath
  csrPath=${certName}.csr
  printf -- "-----BEGIN CERTIFICATE REQUEST-----\n%s\n-----END CERTIFICATE REQUEST-----\n" "$csr" > ${csrPath}
  local signedCertPath
  signedCertPath="${certName}.crt"

  # issue certificate chain with CSR
  openssl x509 -req -extfile <(printf "basicConstraints=critical,CA:FALSE\nkeyUsage=critical,digitalSignature") -days 36500 -in "$csrPath" -CA inter2.crt -CAkey inter2.key -CAcreateserial -out "$signedCertPath"
  certChainPath="${certName}-chain.crt"
  cat "$signedCertPath" inter2.crt inter1.crt ca.crt > "$certChainPath"

  # merge certificate chain
  az keyvault certificate pending merge --vault-name "$AKV_NAME" --name "$certName" --file "$certChainPath"
  echo "merged ${certName}"
}

function createPartialPKCS12CertificateChain() {
  local certName
  certName="partial-pkcs12-cert-chain"
  local policy
  policy=$(cat << EOF
{
  "keyProperties": {
    "exportable": false
  },
  "secretProperties": {
    "contentType": "application/x-pkcs12"
  },
  "x509CertificateProperties": {
    "ekus": [
        "1.3.6.1.5.5.7.3.3"
    ]
  }
}
EOF
)
  az keyvault certificate import --file ./partial-cert-chain.pfx -n "$certName" --vault-name "$AKV_NAME" --policy "$policy"
  echo "imported ${certName}"
}

function createPartialPEMCertificateChain() {
  local certName
  certName="partial-pem-cert-chain"
  local policy
  policy=$(cat << EOF
{
  "keyProperties": {
    "exportable": false
  },
  "secretProperties": {
    "contentType": "application/x-pem-file"
  },
  "x509CertificateProperties": {
    "ekus": [
        "1.3.6.1.5.5.7.3.3"
    ]
  }
}
EOF
)
  az keyvault certificate import --file ./partial-cert-chain.pem -n "$certName" --vault-name "$AKV_NAME" --policy "$policy"
  echo "imported ${certName}"
}

function main() {
  az account set --subscription "$AKV_SUB"

  # self signed certificate
  createSelfSignedPKCS12
  createSelfSignedPEM

  # certificate chain
  mkdir -p ./bin/certs && cd ./bin/certs
  generateCertChain
  # import local certificate chain to Azure Key Vault
  importCAIssuedPKCS12CertificateChain
  importCAIssuedPEMCertificateChain

  # # issue certificate chain with Azure Key Vualt CSR
  createPKCS12CertificateChainWithCSR
  createPEMCertificateChainWithCSR

  # partial certificate chain
  createPartialPKCS12CertificateChain
  createPartialPEMCertificateChain
}

main