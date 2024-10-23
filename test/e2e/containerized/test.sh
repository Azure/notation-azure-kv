#!/bin/bash
#
# containerized e2e test for azure-kv plugin
# prerequisite: 
#   - notation-akv:v1 image

set -e

function testSign(){
    # print all the arguments
    echo "notation sign --signature-format cose localhost:5000/hello-world:v1 --plugin azure-kv" "$@"
    docker run \
        -v "$(pwd)"/test/:/test \
        --network host notation-akv:v1 \
        notation sign --signature-format cose localhost:5000/hello-world:v1 --plugin azure-kv "$@"
    local result=$?
    echo ""
    return $result
}

function assertSucceeded(){
    if [ $? -ne 0 ]; then
        echo "test failed"
        exit 1
    fi
}

function assertFailed(){
    if [ $? -eq 0 ]; then
        echo "test failed"
        exit 1
    fi
}

set +e
echo "start notation azure-kv plugin containerized test"
testSign --id https://acrci-test-kv.vault.azure.net/keys/self-signed-pkcs12/70747b2064c0488e936eba7a29acc4c6 --plugin-config self_signed=true
assertSucceeded
testSign --id https://acrci-test-kv.vault.azure.net/keys/self-signed-pem/a2c329545a934f0aaf434afe64bb392d --plugin-config self_signed=true
assertSucceeded
testSign --id https://acrci-test-kv.vault.azure.net/keys/imported-ca-issued-pem/5a768b6209564c3cb30ecc30d800dc43
assertSucceeded
testSign --id https://acrci-test-kv.vault.azure.net/keys/imported-ca-issued-pem-unordered/c0dcfcda9a454880aec242c70dcb1e2a
assertSucceeded
testSign --id https://acrci-test-kv.vault.azure.net/keys/imported-ca-issued-pkcs12/20548a2bcaba42308f609df2d79682b5
assertSucceeded
testSign --id https://acrci-test-kv.vault.azure.net/keys/imported-ca-issued-pkcs12-unordered/b4fdf86062e44839b666ce8ff3f3a470 
assertSucceeded
testSign --id https://acrci-test-kv.vault.azure.net/keys/csr-ca-issued-pem-chain/09cd1aeaaa894e60b0ef83f062604863
assertSucceeded
testSign --id https://acrci-test-kv.vault.azure.net/keys/csr-ca-issued-pkcs12-chain/aad06a96a2684d6ab79a4ad84cbe917e
assertSucceeded
testSign --id https://acrci-test-kv.vault.azure.net/keys/partial-pem-cert-chain/bf6299c95b96492894be0230935bdab8 --plugin-config ca_certs=/test/e2e/certs/cert-bundle.pem
assertSucceeded
testSign --id https://acrci-test-kv.vault.azure.net/keys/partial-pkcs12-cert-chain/c90493832b4148ee80e2aa10ada67a0b --plugin-config ca_certs=/test/e2e/certs/cert-bundle.pem
assertSucceeded

testSign --id https://acrci-test-kv.vault.azure.net/keys/partial-pem-cert-chain/bf6299c95b96492894be0230935bdab8 --plugin-config ca_certs=./test/e2e/certs/root.pem
assertFailed
testSign --id https://acrci-test-kv.vault.azure.net/keys/partial-pem-cert-chain/bf6299c95b96492894be0230935bdab8
assertFailed
testSign --id https://acrci-test-kv.vault.azure.net/keys/imported-ca-issued-pem/5a768b6209564c3cb30ecc30d800dc43 --plugin-config self_signed=true
assertFailed
testSign --id https://acrci-test-kv.vault.azure.net/keys/imported-ca-issued-pem/5a768b6209564c3cb30ecc30d800dc43 --plugin-config self_signed=true --plugin-config ca_certs=./test/e2e/certs/cert-bundle.pem
assertFailed