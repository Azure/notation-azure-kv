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
6. Configure Kubectl

    ```bash
    rgName="$(terraform output -raw azure_resource_group_name)"
    aksName="$(terraform output -raw azure_kubernetes_cluster_name)"
    az aks get-credentials --name $aksName --resource-group $rgName --admin --only-show-errors --overwrite-existing
    ```

> **Warning**
> **Changes will be made to your local system**. Terraform state files will be stored to disk.

## Deploy Gatekeeper to AKS

1. Add the Gatekeeper repository to deploy the Helm chart.
    ```bash
    helm repo add gatekeeper https://open-policy-agent.github.io/gatekeeper/charts
    ```
2. Run the `helm` install` command to install Gatekeeper
    ```bash
    helm install gatekeeper/gatekeeper  \
    --name-template=gatekeeper \
    --namespace gatekeeper-system --create-namespace \
    --set enableExternalData=true
    ```

## Deploy Ratify to AKS

1. Create an Azure Container Registry token for your AKS cluster to access container images within the regsitry.
    ```bash
    registry="$(terraform output -raw azure_container_registry_name)"

    tokenPassword=$(az acr token create \
        --name exampleToken \
        --registry $registry \
        --scope-map _repositories_admin \
        --query 'credentials.passwords[0].value' \
        --only-show-errors \
        --output tsv)
    ```
2. Use `kubectl` to create a Kubernetes secret

    ```bash
    kubectl create secret docker-registry regcred \
    --docker-server=$registry \
    --docker-username=exampleToken \
    --docker-password=$tokenPassword \
    --docker-email=someone@example.com
    ```

3. Add the Ratify repository to your Helm configuration

    ```bash
    helm repo add ratify https://deislabs.github.io/ratify
    ```

4. Install the Ratify with Helm

    ```bash
    publickey="$(terraform output -raw notary_signing_cert_data)"
    
    helm install ratify ratify/ratify \
    --set registryCredsSecret=regcred \
    --set ratifyTestCert="$publickey"
    ```

5. Deploy the Ratify templates

    ```bash
    curl -L https://deislabs.github.io/ratify/library/default/template.yaml -o template.yaml;
    curl -L https://deislabs.github.io/ratify/library/default/samples/constraint.yaml -o constraint.yaml;
    kubectl apply -f template.yaml;
    kubectl apply -f constraint.yaml;
    rm template.yaml constraint.yaml
    ```

## Install Notation

1. Download the binary.
    ```bash
    curl -Lo notation.tar.gz https://github.com/notaryproject/notation/releases/download/v0.11.0-alpha.4/notation_0.11.0-alpha.4_linux_amd64.tar.gz
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
    ```

## Install the Azure Key Vault plugin

1. Download the plugin.
    ```bash
    curl -Lo notation-azure-kv.tar.gz \
    https://github.com/Azure/notation-azure-kv/releases/download/v0.4.0-beta.1/notation-azure-kv_0.4.0-beta.1_linux_amd64.tar.gz
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
    keyName="$(terraform output -raw signing_cert_name)"
    acrName="$(terraform output -raw azure_container_registry_name)"
    kvName="$(terraform output -raw azure_keyvault_name)"
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
    ```

## Build and sign a container image

Once the Terraform configuration is complete use the [Notation](https://github.com/notaryproject/notation) to sign a container image, then deploy that container to your AKS cluster. 


1. Export the required notation envrionment variables

    ```bash
    export NOTATION_USERNAME=exampleToken
    export NOTATION_PASSWORD=$tokenPassword
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
    notation sign --key $keyName $acrName.azurecr.io/$IMAGE 
    ```

4. Deploy the signed container image

    ```bash
    kubectl create namespace demo;
    kubectl run net-monitor --image=$acrName.azurecr.io/$IMAGE --namespace demo;
    sleep 5;
    kubectl get pods -n demo;
    ```