param location string = resourceGroup().location
param kvName string = 'myakv-${uniqueString(resourceGroup().id)}'
param spObjectId string = ''
param objectId string = ''

param AZURE_CLIENT_ID string = ''
param AZURE_TENANT_ID string = ''
@secure()
param AZURE_CLIENT_SECRET string = ''


// Create a keyvault
resource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: kvName
  location: location
  properties: {
    tenantId: subscription().tenantId
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: spObjectId
        permissions: {
          keys: [
            'get'
            'sign'
          ]
          secrets: [
            'list'
            'get'
          ]
          certificates: [
            'get'
            'create'
          ]
        }
      }
      {
        tenantId: subscription().tenantId
        objectId: objectId
        permissions: {
          keys: [
            'get'
            'sign'
          ]
          secrets: [
            'list'
            'set'
            'get'
          ]
          certificates: [
            'get'
            'create'
            'list'
          ]
        }
      }
    ]
    sku: {
      name: 'standard'
      family: 'A'
    }
  }

  // create secret
  resource azure_client_id 'secrets@2021-11-01-preview' = {
    name: 'AZURE-CLIENT-ID'
    properties: {
      value: AZURE_CLIENT_ID
    }
  }

  resource azure_client_secret 'secrets@2021-11-01-preview' = {
    name: 'AZURE-CLIENT-SECRET'
    properties: {
      value: AZURE_CLIENT_SECRET
    }
  }

  resource azure_tenant_id 'secrets@2021-11-01-preview' = {
    name: 'AZURE-TENANT-ID'
    properties: {
      value: AZURE_TENANT_ID
    }
  }
}

output keyVaultName string = kvName
output keyVaultUri string = keyVault.properties.vaultUri
output keyVaultId string = keyVault.id
