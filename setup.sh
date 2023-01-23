#!/bin/bash
# This script will run an ARM template deployment to deploy all the
# required resources for Notary v2 - remote signing and verification with 
# Notation, Gatekeeper, Ratify, and AKS.

# Requirements:
# Git
# Azure CLI (log in)
# Helm
# Kubectl

# Get the latest version of Kubernetes available in specified location
function getLatestK8s {
   versions=$(az aks get-versions --location $location --output tsv --query="orchestrators[].orchestratorVersion")
   latestVersion=$(printf '%s\n' "${versions[@]}" |
   awk '$1 > m || NR == 1 { m = $1 } END { print m }')

   echo $latestVersion
}

# Add ~/bin to PATH for the script only (From scott)
export PATH=$PATH:~/bin

# Required user input for keyName and keySubjectName
while [[ -z "$keyName" ]] ; do
    echo ''
    echo "A keyname is required."
    echo ''
    read -p "Enter the key name used to sign and verify. I.E. contoso-io: " keyName
done

while [[ -z "$keySubjectName" ]] ; do
    echo ''
    echo "A key subject name is required."
    echo ''
    read -p "Enter the key subject name. I.E. contoso.com: " keySubjectName
done

# Environment variables / positional parameters and defaults.
rgName=$1
rgName=${rgName:-myakv-akv-rg}

# The location to store the meta data for the deployment.
location=${location:-southcentralus} # Currently only region to support premium ACR with Zone Redundancy

# The version of k8s control plane
k8sversion=$2
k8sversion=${k8sversion:-$(getLatestK8s)}

# Grab OS Version
osVersion=$(uname | tr '[:upper:]' '[:lower:]')

# Install Notation CLI
function getNotationProject {
    echo ''
    echo "Setting up the Notation CLI..."

    # Check if ~/bin is there, if not create it 
    [ -d ~/bin ] || mkdir ~/bin

    # Download Notation from pre-release
    curl -Lo notation.tar.gz https://github.com/notaryproject/notation/releases/download/v1.0.0-rc.1/notation_1.0.0-rc.1_linux_amd64.tar.gz > /dev/null 2>&1

    # Extract notation
    mkdir ./tmp
    tar xvzf notation.tar.gz -C ./tmp > /dev/null 2>&1

    # Copy the notation cli to your bin directory
    # tar xvzf ./tmp/notation_0.0.0-SNAPSHOT-${commit}_${osVersion}_amd64.tar.gz -C ~/bin notation > /dev/null 2>&1
    cp ./tmp/notation $HOME/bin 

    # Add $HOME/bin to $PATH
    export PATH="$HOME/bin:$PATH"

    # Clean up
    rm -rf ./tmp notation.tar.gz
}

# Install Azure Keyvault Plugin
function installNotationKvPlugin {
    echo ''
    echo "Setting up the Notation Keyvault Plugin..."

    # Create a directory for the plugin
    [ -d ~/.config/notation/plugins/azure-kv ] || mkdir -p ~/.config/notation/plugins/azure-kv

    # Download the plugin
    curl -Lo notation-azure-kv.tar.gz \
        https://github.com/Azure/notation-azure-kv/releases/download/v0.5.0-rc.1/notation-azure-kv_0.5.0-rc.1_${osVersion}_amd64.tar.gz > /dev/null 2>&1

    # Extract to the plugin directory    
    tar xvzf notation-azure-kv.tar.gz -C ~/.config/notation/plugins/azure-kv notation-azure-kv > /dev/null 2>&1

    # Add Azure Keyvault plugin to notation
    notation plugin ls | grep azure-kv > /dev/null
    if [[ $? -eq 1 ]]
    then 
        notation plugin add azure-kv ~/.config/notation/plugins/azure-kv/notation-azure-kv
    fi

    # List Notation plugins
    notation plugin ls

    # Clean up
    rm -rf notation-azure-kv.tar.gz
}

function spCreate {
    echo ''
    echo "Checking for existing KeyVault service principals"
    echo ''

    check=$(az ad sp list --show-mine --query '[].appDisplayName' --only-show-errors --output tsv | grep "https://mykv")

    if [[ $? -eq 0 ]]
    then
        echo ''
        echo "Service principal $check found! Reusing existing Service Principal..."
        echo ''
        SP_NAME=$check
    else
        echo ''
        echo "No exisiting service principals found. Creating a new one..."
        echo ''
        uniqueString=$(echo $RANDOM)
        SP_NAME=https://mykv${uniqueString}-sp
    fi

    # Create the service principal, capturing the password
    export AZURE_CLIENT_SECRET=$(az ad sp create-for-rbac --skip-assignment --name $SP_NAME --query "password" --only-show-errors --output tsv) && echo "Azure Client Secret: $AZURE_CLIENT_SECRET"

    # Capture the service principal appId
    export AZURE_CLIENT_ID=$(az ad sp list --display-name $SP_NAME --query "[].appId" --only-show-errors --output tsv) && echo "Azure Client ID: $AZURE_CLIENT_ID"

    # Capture the Azure Tenant ID
    export AZURE_TENANT_ID=$(az account show --query "tenantId" --only-show-errors --output tsv) && echo "Azure Tenant ID: $AZURE_TENANT_ID"

    # Capture the Azure Service Principal Obect ID
    export AZURE_OBJECT_ID=$(az ad sp list --display-name $SP_NAME --query "[].id" --only-show-errors --output tsv) && echo "Azure Object ID: $AZURE_OBJECT_ID"

    # TODO Add SP as a secret; sp name / client ID; query sp in KV
}

function acrTokenCreate {
    # Check if secret exists first
    echo ''
    echo "Checking for existing registry secret in Kubernetes..."
    echo ''
    kubectl get secret regcred > /dev/null 2>&1
    if [[ $? -eq 0 ]]
    then
        echo "Found existing secret, pulling environment variables from Azure KeyVault..."
        export NOTATION_USERNAME=$(az keyvault secret show --name notationUsername --vault-name $keyVaultName --query 'value' --only-show-errors --output tsv)
        export NOTATION_PASSWORD=$(az keyvault secret show --name notationPassword --vault-name $keyVaultName --query 'value' --only-show-errors --output tsv)
    else
        echo "No exisiting secret found. Creating registry secret in Kubernetes and adding to Azure KeyVault..."
        echo ''
        export NOTATION_USERNAME=$acrName'-token'
        export NOTATION_PASSWORD=$(az acr token create \
                --name $NOTATION_USERNAME \
                --registry $acrName \
                --scope-map _repositories_admin \
                --query 'credentials.passwords[0].value' \
                --only-show-errors \
                --output tsv)
        
        az keyvault secret set --name notationUsername --value $NOTATION_USERNAME --vault-name $keyVaultName > /dev/null 2>&1 && echo "Notation username stored in keyvault..."
        echo ''

        az keyvault secret set --name notationPassword --value $NOTATION_PASSWORD --vault-name $keyVaultName > /dev/null 2>&1 && echo "Notation password stored in keyvault..."
        echo ''
        
        kubectl create secret docker-registry regcred \
            --docker-server=$acrName.azurecr.io \
            --docker-username=$NOTATION_USERNAME \
            --docker-password=$NOTATION_PASSWORD \
            --docker-email=someone@example.com 
    fi
}

function installGatekeeper {
    echo ''
    echo "Configuring Gatekeeper on your AKS Cluster..."

    # Add Gatekeeper repo if it doesn't exist
    if helm repo ls | grep gatekeeper ;
    then
        echo ''
        echo "Gatekeeper repo already exists, installing gatekeepr now..."
    else
        helm repo add gatekeeper https://open-policy-agent.github.io/gatekeeper/charts
    fi

    # Install Gatekeeper on AKS Cluster
    helm install gatekeeper/gatekeeper  \
        --name-template=gatekeeper \
        --namespace gatekeeper-system --create-namespace \
        --set enableExternalData=true \
        --set controllerManager.dnsPolicy=ClusterFirst,audit.dnsPolicy=ClusterFirst
}

function createSigningCertforKV {
    echo ''
    echo "Generating signing cert with the following subject name: $keySubjectName..."

    # Create policy json file
    cat <<EOF > ./my_policy.json
    {
        "issuerParameters": {
        "certificateTransparency": null,
        "name": "Self"
        },
        "x509CertificateProperties": {
        "ekus": [
            "1.3.6.1.5.5.7.3.3"
        ],
        "key_usage": [
            "digitalSignature"
        ],
        "subject": "CN=${keySubjectName}",
        "validityInMonths": 12
    }
    }
EOF
    
    # Create the certificate in Azure KeyVault
    echo ''
    echo "Creating certificate in Azure KeyVault..."

    az keyvault certificate create --name $keyName --vault-name $keyVaultName --policy @my_policy.json --only-show-errors

    # Get the Key ID for the newly created Cert
    keyID=$(az keyvault certificate show --vault-name $keyVaultName \
            --name $keyName \
            --query "kid" --only-show-errors --output tsv)
    
    # Use notation to add the key id to the kms keys and certs
    echo ''
    echo "Using Notation to add the key ID to Key Management Service..."
    notation key delete $keyName > /dev/null 2>&1 
    notation key add --plugin azure-kv --id $keyID $keyName

    # Checks and balances
    notation key ls
}

function secureAKSwithRatify {
    echo ''
    echo "Configuring Ratify on your AKS cluster..."

    kubectl create ns demo

    PUBLIC_KEY=$(az keyvault certificate show --name $keyName \
            --vault-name $keyVaultName \
            --query 'cer' \
            --only-show-errors \
            --output tsv | base64 -d | openssl x509 -inform DER)

    # Temporary, until the ratify chart is published
    git clone https://github.com/deislabs/ratify.git $HOME/ratify

    helm install ratify $HOME/ratify/charts/ratify \
        --set registryCredsSecret=regcred \
        --set ratifyTestCert="$PUBLIC_KEY"

    kubectl apply -f $HOME/ratify/charts/ratify-gatekeeper/templates/constraint.yaml

    # Clean up
    rm -rf $HOME/ratify
}

# Build Image and Sign it
function buildImageandSign {
    echo ''
    echo "Testing signing of image..."
    # params
    ACR_REPO=net-monitor
    IMAGE_SOURCE=https://github.com/wabbit-networks/net-monitor.git#main
    IMAGE_TAG=v1
    IMAGE=${ACR_REPO}:$IMAGE_TAG

    # Build and Push a new image using ACR Tasks
    az acr build --registry $acrName -t $IMAGE $IMAGE_SOURCE
    echo "Your image has been succesfully built and can be found at this address: $acrName.azurecr.io/$IMAGE"
    echo ''

    # Sign the container image once built
    notation sign --key $keyName $acrName.azurecr.io/$IMAGE && echo "Image successfullly signed using $keyname"
    echo ''

    # Deploy the newly signed image
    echo "Now deploying the newly signed $ACR_REPO image in the demo namespace..."
    kubectl run net-monitor --image=$acrName.azurecr.io/$IMAGE --namespace demo
    sleep 5
    kubectl get pods -n demo

    echo ''
    echo " --Notation Build, Validate, and Deploy Complete --"
}

function notes {
    echo ''

    cat <<EOF > ./notes.txt
    # Notation, Gatekeeper, Ratify, and AKS Notes

    You have now successfully used Azure Container Registry to build a container image and Notation to sign the container image. Your AKS cluster has been configured with Gatekeeper and Ratify, which will help prevent unsigned images from running on your cluster in the demo namespace. 
    
    If you would like to continue to test signed deployments you may use the following commands:

    # Export local environment variables
    export AZURE_CLIENT_SECRET=$AZURE_CLIENT_SECRET
    export AZURE_CLIENT_ID=$AZURE_CLIENT_ID
    export AZURE_TENANT_ID=$AZURE_TENANT_ID

    export NOTATION_USERNAME=$(az keyvault secret show --name notationUsername --vault-name $keyVaultName --query 'value' --only-show-errors --output tsv)
    export NOTATION_PASSWORD=$(az keyvault secret show --name notationPassword --vault-name $keyVaultName --query 'value' --only-show-errors --output tsv)

    # Build and Push a new image using ACR Tasks
    # Be sure to update imagename with your image name and imagetag with your preferred image tag
    # Be sure to update image_source_here with the context for where your Dockerfiles live
    # For more information on how ACR Build Tasks work, checkout the docs here: https://docs.microsoft.com/en-us/cli/azure/acr?view=azure-cli-latest#az-acr-build

    az acr build --registry $acrName -t <imagename:imagetag> <image_source_here>

    # Sign the container image using notation
    notation sign --key $keyName $acrName.azurecr.io/<imagename:imagetag>

    # Deploy the newly signed image to the demo namespace
    kubectl run <name-of-pod> --image=$acrName.azurecr.io/<imagename:imagetag> --namespace demo

    # When you are done with the resources you created, there is an easy to use clean up script availabe. This script will clean up all resources in your Azure subscription, as well as the service principal created, and the notation keys and certs locally created on your system.

    ./cleanup.sh

    You can access these notes anytime by opening the ./notes.txt file.
EOF
    cat ./notes.txt        
}

# Get outputs of Azure Deployment
function getOutput {
   echo $(az deployment sub show --name $rgName --query "properties.outputs.$1.value" --only-show-errors --output tsv)
}

# Deploy AKS, ACR, and Keyvault with Bicep
function deployInfra {
    # Get ObjectId for user
    objectId=$(az ad signed-in-user show --query id --only-show-errors --output tsv)

    echo ''
    echo "Deploying the required infrastructure with the following..."
    ## TODO Ensure Following variables aren't blank
    echo "Azure Client ID: $AZURE_CLIENT_ID"
    echo "Azure Client Secret: $AZURE_CLIENT_SECRET"
    echo "Azure Tenant ID: $AZURE_TENANT_ID"
    echo "Azure SP Object ID: $AZURE_OBJECT_ID"

    # Deploy the infrastructure
    az deployment sub create --name $rgName \
        --location $location \
        --template-file ./iac/main.bicep \
        --parameters rgName=$rgName \
        --parameters location=$location \
        --parameters k8sversion=$k8sversion \
        --parameters spObjectId=$AZURE_OBJECT_ID \
        --parameters objectId=$objectId \
        --parameters AZURE_CLIENT_ID=$AZURE_CLIENT_ID \
        --parameters AZURE_CLIENT_SECRET=$AZURE_CLIENT_SECRET \
        --parameters AZURE_TENANT_ID=$AZURE_TENANT_ID \
        --output none

    # Check for success
    # TODO flip - check for success rather than error
    if [[ $? -eq 1 ]]
    then
        echo ''
        echo "Something went wrong."
        exit 1
    else 
        # Get all the outputs
        aksName=$(getOutput 'aks_name')
        acrName=$(getOutput 'acr_name')
        keyVaultName=$(getOutput 'keyVault_name')

        # Link ACR and AKS together: TODO: Move to bicep if supported
        # NOTE Slow, adds a lot of time
        az aks update --name $aksName --resource-group $rgName --attach-acr $acrName

        # Add new cluster to local Kube Config
        echo ''
        echo "Adding newly created Kubernetes context to your Kube Config..."
        az aks get-credentials --name $aksName --resource-group $rgName --admin
    fi
}

function setup {
    # getNotationProject
    # installNotationKvPlugin
    spCreate
    deployInfra
    acrTokenCreate
    installGatekeeper
    createSigningCertforKV
    secureAKSwithRatify
    buildImageandSign
    # notes
}

# Call setup function
setup
