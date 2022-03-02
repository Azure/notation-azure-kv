targetScope = 'subscription'

param location string = 'eastus'
param k8sversion string = '1.19.6'
param rgName string = 'myakv-akv-rg'
param objectId string = ''
param spObjectId string = ''
param AZURE_CLIENT_ID string = ''
param AZURE_CLIENT_SECRET string = ''
param AZURE_TENANT_ID string = ''

// TODO: connect aks and acr together
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: rgName
  location: location
}

module acr './acr.bicep' = {
  scope: resourceGroup(rg.name)
  name: 'myakv-acr'
  params: {
    location: location
  }
}

module aks './aks.bicep' = {
  scope: resourceGroup(rg.name)
  name: 'myakv-aks'
  params: {
    k8sversion: k8sversion
    location: location
  }
}

module keyvault './keyvault.bicep' = {
  scope: resourceGroup(rg.name)
  name: 'myakv-keyvault'
  params: {
    location: location
    objectId: objectId
    spObjectId: spObjectId
    AZURE_CLIENT_ID: AZURE_CLIENT_ID
    AZURE_CLIENT_SECRET: AZURE_CLIENT_SECRET
    AZURE_TENANT_ID: AZURE_TENANT_ID
  }
}


output rg_name string = rgName
output aks_name string = aks.outputs.clusterName
output acr_name string = acr.outputs.acrName
output keyVault_name string = keyvault.outputs.keyVaultName
