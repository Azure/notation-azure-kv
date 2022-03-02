param agentCount int = 3
param osDiskSizeGB int = 128
param location string = 'eastus2'
param k8sversion string = '1.19.6'
param agentVMSize string = 'Standard_A2_v2'
param servicePrincipalClientId string = 'msi'

var aksName = 'myaks-${uniqueString(resourceGroup().id)}'
var dnsPrefix = '${aksName}-dns'

resource aks 'Microsoft.ContainerService/managedClusters@2020-09-01' = {
  name: aksName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Free'
  }
  properties: {
    kubernetesVersion: k8sversion
    enableRBAC: true
    dnsPrefix: dnsPrefix
    agentPoolProfiles: [
      {
        name: 'agentpool'
        osDiskSizeGB: osDiskSizeGB
        count: agentCount
        vmSize: agentVMSize
        osType: 'Linux'
        type: 'VirtualMachineScaleSets'
        mode: 'System'
        maxPods: 110
        availabilityZones: [
          '1'
        ]
      }
    ]
    networkProfile: {
      loadBalancerSku: 'standard'
      networkPlugin: 'kubenet'
    }
    apiServerAccessProfile: {
      enablePrivateCluster: false
    }
    addonProfiles: {
      httpApplicationRouting: {
        enabled: true
      }
      azurePolicy: {
        enabled: false
      }
    }
    servicePrincipalProfile: {
      clientId: servicePrincipalClientId
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

output clusterName string = aksName
