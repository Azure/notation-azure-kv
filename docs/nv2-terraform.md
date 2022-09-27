# Notary v2 - Remote signing and verification 

In this readme, you'll learn how to deploy and configure Notary v2 using Gatekeeper and Ratify running on Azure Kubernetes Service to sign and verify container images hosted on Azure Container Registry.

Notary is used to digitally sign a docker container image that's pushed to Azure Container Registry, an OCI compliant registry. [Ratify](https://github.com/deislabs/ratify) then uses that signature to verify the images running on the Kubernetes cluster. That policy is then enforced by [Gatekeeper](https://github.com/open-policy-agent/gatekeeper).

## Prerequisites
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [kubectl](https://kubernetes.io/docs/tasks/tools/#kubectl)
- [Terraform](https://www.terraform.io/downloads)

## Deploy the infrastructure

For secure signing and verification, Notary v2 requires several infrastructure components; an OCI conformant registry, a place to securely access certificates, and a Kubernetes cluster to run containers. To avoid the effort of manually deploying and configuring these components, Terraform has been used to automate the deployment.

Run the following commands:

1. Authenciate to Azure

    ```bash
    az login
    ```

    To learn about different authenticate methods, see [authenticating to azure](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs#authenticating-to-azure).

2. Verify the correct Azure subscription is selected

    ```bash
    az account list

    az account set --subscription="SUBSCRIPTION_ID"
    ```

    Replace `SUBSCRIPTION_ID` with the ID of the appropriate subscription.

3. Change into the `iac/notary` directory

    ```bash
    cd iac/notary/
    ```

4. Initallize Terraform 

    ```bash
    terraform init
    ```

5. Apply the Terraform configuration

    ```
    terraform apply
    ```

    ```output
    Apply complete! Resources: 0 added, 1 changed, 0 destroyed.

    Outputs:

    azure_key_vault_name = "notary-4d32ffd6-kv"
    azure_resource_group_name = "notary-4d32ffd6-rg"
    signing_key_name = "example"
    ```

> **Warning**
> **Changes will be made to your local system**. Terraform state files will be stored to disk and kubectl will be configured to communicate with the AKS cluster deployed.

## Install Notation

1. Download the binary.
    ```bash
    curl -Lo notation.tar.gz https://github.com/notaryproject/notation/releases/download/v0.9.0-alpha.1/notation_0.9.0-alpha.1_linux_amd64.tar.gz
    ```
2. Extract the Notation CLI.
    ```bash
    [ -d ~/bin ] || mkdir ~/bin
    tar xvzf notation.tar.gz -C ~/bin notation
    rm -rf notation.tar.gz
    ```
3. Add Notation to that PATH environment variable.
    ```bash
    export PATH="$HOME/bin:$PATH"
    notation --version
    ```

## Install the Azure Key Vault plugin

1. Download the plugin.
    ```bash
    curl -Lo notation-azure-kv.tar.gz \
    https://github.com/Azure/notation-azure-kv/releases/download/v0.3.1-alpha.1/notation-azure-kv_0.3.1-alpha.1_linux_amd64.tar.gz
    ```
2. Extract the plugin
    ```bash
    [ -d ~/.config/notation/plugins/azure-kv ] || mkdir -p ~/.config/notation/plugins/azure-kv
    tar xvzf notation-azure-kv.tar.gz -C ~/.config/notation/plugins/azure-kv notation-azure-kv > /dev/null 2>&1
    rm -rf notation-azure-kv.tar.gz
    ```
3. Verify the plugin was added.
    ```bash
    notation plugin list
    ```

## Add the signing certificate to Notation

1. Use the output of the `terraform apply` command in the previous step to populate the following variables:

    ```bash
    keyName="$(terraform output -raw signing_key_name)"
    acrName="$(terraform output -raw azure_container_registry)"
    kvName="$(terraform output -raw azure_key_vault_name)"
    ```

2. Download the certificate and capture the key id as a variables:

    ```bash
    certId=$(az keyvault certificate show --name $keyName --vault-name $kvName --query id -o tsv)
    keyId=$(az keyvault certificate show --name $keyName --vault-name $kvName --query kid -o tsv)
    certPath=$keyName-cert.crt
    az keyvault certificate download --file $certPath --id $certId
    ```

3. Add the signing certificate to Notation
    
    ```bash
    notation key add --name $keyName --id $keyId --plugin azure-kv 
    notation cert add --name $keyName $certPath
    notation key list
    notation cert list
    ```

## Build and sign a container image

Once the Terraform configuration is complete use the [Notation](https://github.com/notaryproject/notation) to sign a container image, then deploy that container to your AKS cluster. 


1. Export the required notation envrionment variables

    ```bash
    export NOTATION_USERNAME=$(az keyvault secret show --name NOTATION-USERNAME --vault-name $kvName --query 'value' --only-show-errors --output tsv)
    export NOTATION_PASSWORD=$(az keyvault secret show --name NOTATION-PASSWORD --vault-name $kvName --query 'value' --only-show-errors --output tsv)
    export AZURE_CLIENT_SECRET=$(az keyvault secret show --name AZURE-CLIENT-SECRET --vault-name $kvName --query 'value' --only-show-errors --output tsv)
    export AZURE_CLIENT_ID=$(az keyvault secret show --name AZURE-CLIENT-ID --vault-name $kvName --query 'value' --only-show-errors --output tsv)
    export AZURE_TENANT_ID=$(az keyvault secret show --name AZURE-TENANT-ID --vault-name $kvName --query 'value' --only-show-errors --output tsv)
    ```

    NOTE: Notation uses these environment variables to connect to the Azure Container Registry. Optionally, you could also pass them in as command-line arguments. 


2. Build the container image

    ```bash
    ACR_REPO=net-monitor
    IMAGE_SOURCE=https://github.com/wabbit-networks/net-monitor.git#main
    IMAGE_TAG=v1
    IMAGE=${ACR_REPO}:$IMAGE_TAG

    az acr build --registry $acrName -t $IMAGE $IMAGE_SOURCE
    ```

3. Remotely sign the container image

    ```bash
    notation sign --key $keyName $acrName.azurecr.io/$IMAGE && echo "Image successfullly signed using $keyname"
    ```

4. Deploy the signed container image

    ```bash
    kubectl run net-monitor --image=$acrName.azurecr.io/$IMAGE --namespace demo
    sleep 5
    kubectl get pods -n demo
    ```