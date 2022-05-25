param location string = resourceGroup().location

var acrName = 'myacr${uniqueString(resourceGroup().id)}'

// param keyVaultId string

resource acr 'Microsoft.ContainerRegistry/registries@2021-12-01-preview' = {
  name: acrName
  location: location
  sku: {
    name: 'Premium' 
  }
  properties: {
    adminUserEnabled: true
    zoneRedundancy: 'Enabled'
  }
}

output acrName string = acrName
