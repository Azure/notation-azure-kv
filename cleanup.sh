#!/bin/bash

# Get outputs of Azure Deployment
function getOutput {
   echo $(az deployment sub show --name $rgName --query "properties.outputs.$1.value" --output tsv)
}

# Prompt for Notation Key Name
while read -p "Enter the key name used to sign and verify. I.E. contoso-io: " keyName && [[ -z "$keyName" ]] ; do
    echo ''
    echo "A keyname is required."
    echo ''
done

# Prompt for Resource Group Name
while read -p "Enter your resource group name: (Default is myakv-akv-rg) " rgName && [[ -z "$rgName" ]] ; do
    echo ''
    echo "A resource group name is required."
    echo ''
done

function notationCleanup {
    # Clean up notation keys and certs stored
    echo ''
    notation key remove $keyName > /dev/null 2>&1 && echo "$keyName key successfully deleted..."
    notation cert remove $keyName > /dev/null 2>&1 && echo "$keyName cert successfully deleted..."
}

function spCleanup {
    # Clean up service principals created
    servicePrincipal=$(az ad sp list --show-mine --query '[].appDisplayName' --only-show-errors --output tsv | grep "https://mykv")

    appId=$(az ad sp list --display-name $servicePrincipal --query '[].appId' --only-show-errors --output tsv)

    az ad sp delete --id $appId --only-show-errors && echo "$servicePrincipal succcessfully deleted..."
    echo ''
}

function rgCleanup {
    # Cleanup created resources completely
    # TODO triple check keyVaultName output 
    keyVaultName=$(getOutput 'keyVault_name')
    tenantId=$(az keyvault secret show --name AZURE-TENANT-ID --vault-name $keyVaultName --query 'value' --only-show-errors --output tsv)
    subscriptionId=$(az account show --query id --only-show-errors --output tsv)

    az group delete --name $rgName -y && echo "Resource group deleted..."
    echo ''
    az keyvault purge --subscription $subscriptionId -n $keyVaultName --only-show-errors && echo "Keyvault fully purged..."
}

function cleanup {
    notationCleanup
    spCleanup
    rgCleanup
}

# Call cleanup function
cleanup